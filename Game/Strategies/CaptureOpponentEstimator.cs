using Game.Fast;

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
                baseScore = 100_000_000 - state.playersLeft * 10_000_000;

            var opponentCaptured = state.players[player].opponentTerritoryCaptured - prevCaptured;
            var score = state.players[player].score - prevScore;
            var time = state.time - prevTime;
            if (opponentCaptured > 0)
                return baseScore + 100_000.0 * opponentCaptured - 100.0 * time + score;

            return baseScore + score;
        }
    }
}