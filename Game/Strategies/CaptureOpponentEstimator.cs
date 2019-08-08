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

            if (state.time < Env.MAX_TICK_COUNT - 100)
            {
                baseScore += 1.0 * state.players[player].nitrosCollected * state.config.width * (state.config.ticksPerRequest - state.config.nitroTicksPerRequest) / state.config.ticksPerRequest; 
                baseScore -= 1.0 * state.players[player].slowsCollected * state.config.width * (state.config.slowTicksPerRequest - state.config.ticksPerRequest) / state.config.ticksPerRequest; 
            }

            var opponentCaptured = state.players[player].opponentTerritoryCaptured - prevCaptured;
            var score = state.players[player].score - prevScore;
            var time = state.time - prevTime;
            if (opponentCaptured > 0)
                return baseScore + 100_000.0 * opponentCaptured - 100.0 * time + score;

            return baseScore + score;
        }
    }
}