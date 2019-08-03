using Game.Fast;
using Game.Helpers;
using Game.Protocol;

namespace Game.Strategies
{
    public class BestScoreSpeedEstimator : IEstimator
    {
        private int timeBefore;
        private int scoreBefore;
        
        public void Before(FastState state, int player)
        {
            timeBefore = state.time;
            scoreBefore = state.players[player].score + state.players[player].lineCount * Env.NEUTRAL_TERRITORY_SCORE;
        }

        public double Estimate(FastState state, int player)
        {
            var addScore = 0;
            if (state.players[player].status == PlayerStatus.Eliminated)
                addScore -= 1_000_000_000;
            else
                addScore += 10_000_000 - state.playersLeft * 1_000_000;

            var score = state.players[player].score - scoreBefore;

            // var minAddDist = int.MaxValue;
            // if (state.players[player].lineCount == 0 && state.players[player].status != PlayerStatus.Eliminated)
            // {
            //     for (ushort c = 0; c < state.config.x_cells_count * state.config.y_cells_count; c++)
            //     {
            //         if (state.territory[c] != player)
            //         {
            //             var addDist = state.MDist(c, state.players[player].arrivePos);
            //             if (addDist < minAddDist)
            //                 minAddDist = addDist;
            //         }
            //     }
            // }

            var deltaTime = state.time - timeBefore;
            // if (minAddDist != int.MaxValue)
            //     deltaTime += minAddDist * state.players[player].shiftTime;

            if (deltaTime == 0)
                deltaTime = 1;

            Logger.Debug($"deltaTime: {deltaTime}, score: {score}");
            return addScore + (double)score / deltaTime;
        }
    }
}