using System.Linq;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk.PathEstimators
{
    public class CaptureOpponentEstimator : IPathEstimator
    {
        private readonly bool awardTopKillOnly;
        private int prevCaptured;
        private int prevScore;
        private int prevTime;
        private readonly bool[] topPlayers = new bool[6];

        public CaptureOpponentEstimator(bool awardTopKillOnly)
        {
            this.awardTopKillOnly = awardTopKillOnly;
        }
        
        public void Before(State state, int player)
        {
            prevCaptured = state.players[player].opponentTerritoryCaptured;
            prevScore = state.players[player].score;
            prevTime = state.time;
            if (awardTopKillOnly)
            {
                var tops = state.players.Select((x, i) => new {i, x.score}).OrderByDescending(x => x.score).Take(3).Select(x => x.i).ToHashSet();
                for (var i = 0; i < state.players.Length; i++)
                    topPlayers[i] = tops.Contains(i);
            }
        }

        public double Estimate(State state, int player, int pathStartLen)
        {
            double baseScore;
            if (state.players[player].status == PlayerStatus.Eliminated)
                baseScore = -1_000_000_000;
            else
            {
                baseScore = 0;
                for (var i = 0; i < state.players.Length; i++)
                {
                    if (i != player 
                        && state.players[i].status == PlayerStatus.Eliminated 
                        && (state.players[i].killedBy & (1 << player)) != 0
                        && (!awardTopKillOnly || topPlayers[i]))
                        baseScore += 10_000_000;
                }
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

            return baseScore + score;
        }
    }
}