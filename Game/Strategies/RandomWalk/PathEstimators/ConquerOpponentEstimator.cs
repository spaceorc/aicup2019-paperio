using System;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk.PathEstimators
{
    public class ConquerOpponentEstimator : IPathEstimator
    {
        const int delta = 5;
        
        const double defaultScoreCoeff = 1;
        const double defaultTimeCoeff = 2;
        const double defaultDistCoeff = 5;
        const double defaultTerrCoeff = 10;
        
        private double scoreCoeff;
        private double timeCoeff;
        private double distCoeff;
        private double terrCoeff;

        public ConquerOpponentEstimator()
            : this(defaultScoreCoeff, defaultTimeCoeff, defaultDistCoeff, defaultTerrCoeff)
        {
        }
        
        public ConquerOpponentEstimator(double scoreCoeff, double timeCoeff, double distCoeff, double terrCoeff)
        {
            this.scoreCoeff = scoreCoeff;
            this.timeCoeff = timeCoeff;
            this.distCoeff = distCoeff;
            this.terrCoeff = terrCoeff;
        }
            
        private int prevCaptured;
        private int prevScore;
        private int prevTime;
        private readonly int[] dist = new int[Env.CELLS_COUNT]; 
        private readonly int[] terr = new int[Env.CELLS_COUNT]; 
        private readonly int[] mint = new int[Env.CELLS_COUNT]; 
        
        public void Before(State state, int player)
        {
            prevCaptured = state.players[player].opponentTerritoryCaptured;
            prevScore = state.players[player].score;
            prevTime = state.time;

            for (ushort c = 0; c < Env.CELLS_COUNT; c++)
            {
                mint[c] = int.MaxValue;
                for (ushort nc = 0; nc < Env.CELLS_COUNT; nc++)
                {
                    if (state.territory[nc] != 0xFF && state.territory[nc] != player && state.players[state.territory[nc]].status != PlayerStatus.Eliminated)
                    {
                        var mdist = Coords.MDist(c, nc);
                        if (mdist < mint[c])
                        {
                            mint[c] = mdist;
                        }
                    }
                }
            }

            for (var x = 0; x < Env.X_CELLS_COUNT; x++)
            for (var y = 0; y < Env.Y_CELLS_COUNT; y++)
            {
                var c = V.Get(x, y).ToCoord();
                
                terr[c] = 0;
                for (var dx = -delta; dx <= delta; dx++)
                for (var dy = -delta; dy <= delta; dy++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Env.X_CELLS_COUNT && ny < Env.Y_CELLS_COUNT)
                    {
                        var nc = V.Get(nx, ny).ToCoord();
                        if (state.territory[nc] != 0xFF && state.territory[nc] != player && state.players[state.territory[nc]].status != PlayerStatus.Eliminated)
                            terr[c]++;
                    }
                }
                
                dist[c] = 0;
                var count = 0;
                for (var i = 0; i < state.players.Length; i++)
                {
                    if (i == player || state.players[i].status == PlayerStatus.Eliminated || state.players[i].arrivePos == ushort.MaxValue)
                        continue;
                    var mdist = Coords.MDist(c, state.players[i].arrivePos);
                    dist[c] += mdist * mdist;
                    count++;
                }

                if (count > 0)
                    dist[c] = (int)Math.Sqrt(dist[c] / count);
            }
        }

        public double Estimate(State state, int player, int pathStartLen)
        {
            if (state.players[player].status == PlayerStatus.Eliminated)
                throw new InvalidOperationException();

            var baseScore = 0;
            for (var i = 0; i < state.players.Length; i++)
            {
                if (state.players[i].status == PlayerStatus.Eliminated && (state.players[i].killedBy & (1 << player)) != 0)
                    baseScore += 10_000_000;
            }

            var nitroTimeBonus = state.players[player].nitrosCollected * 30 * (Env.TICKS_PER_REQUEST - Env.NITRO_TICKS_PER_REQUEST);
            var nitroScoreBonus = nitroTimeBonus / Env.TICKS_PER_REQUEST;
            
            var slowTimePenalty = state.players[player].slowsCollected * 30 * (Env.SLOW_TICKS_PER_REQUEST - Env.TICKS_PER_REQUEST);
            var slowScorePenalty = slowTimePenalty / Env.TICKS_PER_REQUEST;
            
            var opponentCaptured = state.players[player].opponentTerritoryCaptured - prevCaptured;
            if (pathStartLen > 0 && opponentCaptured == 0)
                return int.MinValue;

            var score = state.players[player].score - prevScore;
            var time = state.time - prevTime;
            
            if (state.time < Env.MAX_TICK_COUNT - 100)
            {
                time += slowTimePenalty - nitroTimeBonus;
                score += nitroScoreBonus - slowScorePenalty;
            }

            if (opponentCaptured > 0)
                return baseScore + 100_000.0 * opponentCaptured - 100.0 * time + score;

            var captured = state.capture.territoryCaptureCount[player];
            if (captured == 0)
                return baseScore + 0.1 * score;

            baseScore += 10_000;

            var mind = int.MaxValue;
            for (int i = 0; i < captured; i++)
            {
                var d = mint[state.capture.territoryCapture[player, i]];
                if (d < mind)
                    mind = d;
            }
            
            return baseScore
                   - 100.0 * mind
                   - 1.0 * time
                   + 0.1 * score;
        }
    }
}