using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.BruteForce
{
    public class AllowedDirectionsFinder : IMinimaxEstimator
    {
        private readonly Minimax minimax;

        public AllowedDirectionsFinder()
        {
            minimax = new Minimax(this, 1);
        }

        public byte GetAllowedDirectionsMask(ITimeManager timeManager, State state, int player)
        {
            var top = state.players[player].dir == null ? 6 : 5;

            byte result = 0;
            for (byte d = 3; d <= top; d++)
            {
                var action = (Direction)(((byte)(state.players[player].dir ?? Direction.Up) + d) % 4);
                minimax.Alphabeta(timeManager, state, player, action);
                if (minimax.bestScore > 0)
                    result = (byte)(result | (1 << (int)(action)));
            }

            return result;
        }

        public double Estimate(State state, int player)
        {
            return state.players[player].status == PlayerStatus.Eliminated ? -1 : 1;
        }
    }
}