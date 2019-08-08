using Game.Fast;

namespace Game.Strategies
{
    public class BestScoreEstimator : IEstimator
    {
        public void Before(FastState state, int player)
        {
        }

        public double Estimate(FastState state, int player)
        {
            var score = 0;
            if (state.players[player].status == PlayerStatus.Eliminated)
                score -= 1_000_000_000;
            else
            {
                for (int i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated && (state.players[i].killedBy & (1 << player)) != 0)
                        score += 1_000_000;
                }
            }

            score += state.players[player].score;

            // if (state.time < Env.MAX_TICK_COUNT - 100)
            // {
            //     score += state.players[player].nitrosCollected * 20;
            //     score -= state.players[player].slowsCollected * 50;
            // }

            // score += state.players[player].nitrosCollected * 30 * (state.config.ticksPerRequest - state.config.nitroTicksPerRequest) / state.config.ticksPerRequest; 
            // score -= state.players[player].slowsCollected * 30 * (state.config.slowTicksPerRequest - state.config.ticksPerRequest) / state.config.ticksPerRequest; 

            return score;
        }
    }
}