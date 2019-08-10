using Game.Fast;
using Game.Helpers;
using Game.Types;

namespace Game.AlterStaregy
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

            if (depth == 0 || state.isGameOver || state.players[player].status == PlayerStatus.Eliminated)
            {
                estimations++;
                UndoData undo = null;
                if (activePlayer != player && !state.isGameOver)
                {
                    for (int p = activePlayer; p < state.players.Length; p++)
                    {
                        if (p == player
                            || state.players[p].arriveTime != 0
                            || state.players[p].status == PlayerStatus.Eliminated
                            || state.players[p].status == PlayerStatus.Broken)
                            continue;

                        for (byte d = 3; d <= 5; d++)
                        {
                            var action = (Direction)(((byte)state.players[p].dir.Value + d) % 4);
                            var ne = state.NextCoord(state.players[p].arrivePos, action);
                            if (ne != ushort.MaxValue)
                            {
                                commands[commandsStart + p] = action;
                                if (state.territory[ne] == p)
                                    break;
                            }
                        }
                    }

                    undo = state.NextTurn(commands, withUndo: true, commandsStart: commandsStart);
                }

                var score = estimator.Estimate(state, player);
                if (undo != null)
                    state.Undo(undo);

                return score;
            }

            var top = state.players[activePlayer].dir == null ? 6 : 5;

            for (byte d = 3; d <= top; d++)
            {
                var action = (Direction)(((byte)(state.players[activePlayer].dir ?? Direction.Up) + d) % 4);
                commands[commandsStart + activePlayer] = action;
                int nextPlayer = activePlayer == player ? 0 : activePlayer + 1;
                for (; nextPlayer < state.players.Length; nextPlayer++)
                {
                    if (nextPlayer == player
                        || state.players[nextPlayer].status == PlayerStatus.Eliminated
                        || state.players[nextPlayer].status == PlayerStatus.Broken)
                        continue;
                    break;
                }

                UndoData undo = null;
                if (nextPlayer == state.players.Length)
                {
                    undo = state.NextTurn(commands, withUndo: true, commandsStart:commandsStart);
                    nextPlayer = player;
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