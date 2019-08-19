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
        public const int killScore = 1000;

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

        public double Estimate(State state, int player, InterestingFacts facts)
        {
            if (state.players[player].status == PlayerStatus.Eliminated)
                return double.MinValue;
            
            if (state.isGameOver && facts.places[player] == 0)
                return double.MaxValue;

            var score = 1;
            for (var i = 0; i < state.players.Length; i++)
            {
                if (i != player
                    && state.players[i].status == PlayerStatus.Eliminated
                    && (state.players[i].killedBy & (1 << player)) != 0)
                    score += killScore;
            }

            return score;
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