using Game.Fast;
using Game.Protocol;

namespace Game.AlterStaregy
{
    public class MinimaxEstimator : IMinimaxEstimator
    {
        public double Estimate(FastState state, int player)
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
            
            var score = state.players[player].score;
            
            if (state.time < Env.MAX_TICK_COUNT - 100)
                score += nitroScoreBonus - slowScorePenalty;
            
            return baseScore + score;
        }
    }
}