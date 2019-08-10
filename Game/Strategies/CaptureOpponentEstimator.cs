using Game.Fast;
using Game.Protocol;

namespace Game.Strategies
{
    public class CaptureOpponentEstimator : IEstimator
    {
        private int prevCaptured;
        private int prevScore;
        private int prevTime;
        
        public void Before(FastState state, int player)
        {
            prevCaptured = state.players[player].opponentTerritoryCaptured;
            prevScore = state.players[player].score;
            prevTime = state.time;
        }

        public double Estimate(FastState state, int player, int pathStartLen)
        {
            double baseScore;
            if (state.players[player].status == PlayerStatus.Eliminated)
                baseScore = -1_000_000_000;
            else
            {
                baseScore = 0;
                for (int i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated && (state.players[i].killedBy & (1 << player)) != 0)
                        baseScore += 10_000_000;
                }
            }

            var nitroTimeBonus = state.players[player].nitrosCollected * 30 * (state.config.ticksPerRequest - state.config.nitroTicksPerRequest);
            var nitroScoreBonus = nitroTimeBonus / state.config.ticksPerRequest;
            
            var slowTimePenalty = state.players[player].slowsCollected * 30 * (state.config.slowTicksPerRequest - state.config.ticksPerRequest);
            var slowScorePenalty = slowTimePenalty / state.config.ticksPerRequest;
            
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