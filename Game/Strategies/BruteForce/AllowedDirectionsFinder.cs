using System.Text;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Strategies.RandomWalk;

namespace Game.Strategies.BruteForce
{
    public class AllowedDirectionsFinder : IMinimaxEstimator
    {
        public readonly Minimax minimax;

        public AllowedDirectionsFinder(int maxDepth)
        {
            minimax = new Minimax(this, maxDepth);
        }

        public byte GetAllowedDirectionsMask(ITimeManager timeManager, State state, int player, DistanceMap distanceMap, InterestingFacts facts)
        {
            var result = (byte)0;
            minimax.Alphabeta(timeManager, state, player, facts.pathsToOwned, distanceMap, facts);
            for (int i = 0; i < 4; i++)
            {
                if (minimax.bestResultScores[i] > 0)
                    result = (byte)(result | (1 << i));
            }
            return result;
        }

        public double Estimate(State state, int player)
        {
            return state.players[player].status == PlayerStatus.Eliminated ? double.MinValue : double.MaxValue;
        }
        
        public static string DescribeAllowedDirectionsMask(byte allowedDirectionsMask)
        {
            var result = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                if ((allowedDirectionsMask & (1 << i)) != 0)
                    result.Append(((Direction)i).ToString()[0]);
            }

            return result.ToString();
        }

    }
}