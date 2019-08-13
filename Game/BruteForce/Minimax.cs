using Game.Fast;
using Game.Helpers;
using Game.Types;

namespace Game.BruteForce
{
    public class Minimax
    {
        private readonly IMinimaxEstimator estimator;
        private readonly int maxDepth;

        public int estimations;
        public double bestScore;
        public int bestDepth;
        public Direction bestAction;
        public Direction[] commands;

        public Minimax(IMinimaxEstimator estimator, int maxDepth)
        {
            this.estimator = estimator;
            this.maxDepth = maxDepth;
            commands = new Direction[6 * maxDepth];
        }

        public void Alphabeta(ITimeManager timeManager, FastState state, int player)
        {
            bestScore = double.MinValue;
            bestDepth = 0;
            estimations = 0;
            bestAction = default(Direction);

            var depth = 1;
            while (!timeManager.IsExpired && depth <= maxDepth)
            {
                Direction action;
                var score = Alphabeta(timeManager, state, player, depth, player, double.MinValue, double.MaxValue, out action, 0);
                if (double.IsNegativeInfinity(score))
                    break;
                bestScore = score;
                bestAction = action;
                bestDepth = depth;
                depth++;
            }
        }

        private double Alphabeta(
            ITimeManager timeManager,
            FastState state,
            int player,
            int depth,
            int activePlayer,
            double a,
            double b,
            out Direction resultAction,
            int commandsStart)
        {
            resultAction = default(Direction);
            if (timeManager.IsExpired)
                return double.NegativeInfinity;

            if (depth == 0 || state.isGameOver || state.players[player].status == PlayerStatus.Eliminated || activePlayer == state.players.Length)
            {
                estimations++;
                var score = estimator.Estimate(state, player);
                return score;
            }

            var top = state.players[activePlayer].dir == null ? 6 : 5;

            for (byte d = 3; d <= top; d++)
            {
                var action = (Direction)(((byte)(state.players[activePlayer].dir ?? Direction.Up) + d) % 4);
                var ne = state.NextCoord(state.players[activePlayer].arrivePos, action);
                if (ne == ushort.MaxValue || (state.lines[ne] & (1 << activePlayer)) != 0)
                    continue;
                
                commands[commandsStart + activePlayer] = action;
                int nextPlayer = activePlayer == player ? 0 : activePlayer + 1;
                for (; nextPlayer < state.players.Length; nextPlayer++)
                {
                    if (nextPlayer == player
                        || state.players[nextPlayer].status == PlayerStatus.Eliminated
                        || state.players[nextPlayer].status == PlayerStatus.Broken
                        || state.players[nextPlayer].arriveTime > 0)
                        continue;
                    break;
                }

                UndoData undo = null;
                if (nextPlayer == state.players.Length)
                {
                    undo = state.NextTurn(commands, withUndo: true, commandsStart:commandsStart);
                    if (state.players[player].status != PlayerStatus.Eliminated && state.players[player].arriveTime == 0)
                        nextPlayer = player;
                    else
                    {
                        for (nextPlayer = 0; nextPlayer < state.players.Length; nextPlayer++)
                        {
                            if (nextPlayer == player
                                || state.players[nextPlayer].status == PlayerStatus.Eliminated
                                || state.players[nextPlayer].status == PlayerStatus.Broken
                                || state.players[nextPlayer].arriveTime > 0)
                                continue;
                            break;
                        }
                    }

                    commandsStart += 6;
                }

                var score = Alphabeta(timeManager, state, player, depth - 1, nextPlayer, a, b, out _, commandsStart);

                if (undo != null)
                {
                    state.Undo(undo);
                    commandsStart -= 6;
                }

                if (double.IsNegativeInfinity(score))
                    return double.NegativeInfinity;

                if (player == activePlayer)
                {
                    if (score > a)
                    {
                        a = score;
                        resultAction = action;
                    }
                }
                else
                {
                    if (score < b)
                    {
                        b = score;
                        resultAction = action;
                    }
                }

                if (a >= b)
                    break;
            }

            return player == activePlayer ? a : b;
        }
    }
}