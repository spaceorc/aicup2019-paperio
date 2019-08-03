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
                score += 10_000_000 - state.playersLeft * 1_000_000;

            score += state.players[player].score;
            return score;
        }
    }
}