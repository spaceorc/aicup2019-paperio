using Game.Protocol;

namespace Game.Unsafe
{
    public unsafe class UnsafeMinimaxEstimator : IUnsafeMinimaxEstimator
    {
        public double Estimate(UnsafeState* state, int player)
        {
            var players = (UnsafePlayer*)state->players;
            
            double baseScore;
            if (players[player].status == UnsafePlayer.STATUS_ELIMINATED)
                baseScore = -1_000_000_000;
            else
            {
                baseScore = 0;
                for (int i = 0; i < state->playersCount; i++)
                {
                    if (players[i].status == UnsafePlayer.STATUS_ELIMINATED && (players[i].killedBy & (1 << player)) != 0)
                        baseScore += 10_000_000;
                }
            }
            
            var nitroTimeBonus = players[player].nitrosCollected * 30 * (Env.TICKS_PER_REQUEST - Env.NITRO_TICKS_PER_REQUEST);
            var nitroScoreBonus = nitroTimeBonus / Env.TICKS_PER_REQUEST;
            
            var slowTimePenalty = players[player].slowsCollected * 30 * (Env.SLOW_TICKS_PER_REQUEST - Env.TICKS_PER_REQUEST);
            var slowScorePenalty = slowTimePenalty / Env.TICKS_PER_REQUEST;
            
            var score = (int)players[player].score;
            
            var otherScore = 0;
            var otherMaxScore = 0;
            for (int i = 0; i < state->playersCount; i++)
            {
                if (i != player)
                {
                    otherScore += players[i].score;
                    if (players[i].score > otherMaxScore)
                        otherMaxScore = players[i].score;
                }
            }
            
            if (state->time < Env.MAX_TICK_COUNT - 100)
                score += nitroScoreBonus - slowScorePenalty;
            
            return baseScore + 1000.0 * score - otherScore - 3.0 * otherMaxScore;
        }
    }
}