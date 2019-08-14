using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.BruteForce
{
    public class MinimaxEstimator : IMinimaxEstimator
    {
        public double Estimate(State state, int player)
        {
            double baseScore;
            if (state.players[player].status == PlayerStatus.Eliminated)
                baseScore = -1_000_000_000;
            else
            {
                baseScore = 0;
                for (var i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated && (state.players[i].killedBy & (1 << player)) != 0)
                        baseScore += 10_000_000;
                }
            }
            
            var nitroTimeBonus = state.players[player].nitrosCollected * 30 * (Env.TICKS_PER_REQUEST - Env.NITRO_TICKS_PER_REQUEST);
            var nitroScoreBonus = nitroTimeBonus / Env.TICKS_PER_REQUEST;
            
            var slowTimePenalty = state.players[player].slowsCollected * 30 * (Env.SLOW_TICKS_PER_REQUEST - Env.TICKS_PER_REQUEST);
            var slowScorePenalty = slowTimePenalty / Env.TICKS_PER_REQUEST;
            
            var score = state.players[player].score;
            
            var otherScore = 0;
            var otherMaxScore = 0;
            for (var i = 0; i < state.players.Length; i++)
            {
                if (i != player)
                {
                    otherScore += state.players[i].score;
                    if (state.players[i].score > otherMaxScore)
                        otherMaxScore = state.players[i].score;
                }
            }
            
            if (state.time < Env.MAX_TICK_COUNT - 100)
                score += nitroScoreBonus - slowScorePenalty;
            
            return baseScore + 1000.0 * score - otherScore - 3.0 * otherMaxScore;
        }
    }
}