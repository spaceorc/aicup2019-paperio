using Game.Helpers;
using Game.Protocol;

namespace Game.Unsafe
{
    public unsafe class UnsafeMinimax
    {
        private readonly IUnsafeMinimaxEstimator estimator;
        private readonly int maxDepth;

        public int estimations;
        public double bestScore;
        public int bestDepth;
        public byte bestAction;

        public UnsafeMinimax(IUnsafeMinimaxEstimator estimator, int maxDepth)
        {
            this.estimator = estimator;
            this.maxDepth = maxDepth;
            estimations = 0;
            bestScore = 0;
            bestDepth = 0;
            bestAction = 0;
        }

        public void Alphabeta(ITimeManager timeManager, UnsafeState* state, int player)
        {
            bestScore = double.MinValue;
            bestDepth = 0;
            estimations = 0;
            bestAction = UnsafePlayer.DIR_NULL;

            var unsafeCapture = new UnsafeCapture();
            unsafeCapture.Init();

            var depth = 1;
            while (!timeManager.IsExpired && depth <= maxDepth)
            {
                byte action;
                var score = Alphabeta(timeManager, state, player, depth, player, double.MinValue, double.MaxValue, out action, &unsafeCapture);
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
            UnsafeState* state,
            int player,
            int depth,
            int activePlayer,
            double a,
            double b,
            out byte resultAction,
            UnsafeCapture* capture)
        {
            var undo = default(UnsafeUndo);
            resultAction = UnsafePlayer.DIR_NULL;
            if (timeManager.IsExpired)
                return double.NegativeInfinity;

            var players = (UnsafePlayer*)state->players;
            if (state->time > Env.MAX_TICK_COUNT 
                || players[player].status == UnsafePlayer.STATUS_ELIMINATED
                || depth == 0 && activePlayer == player)
            {
                estimations++;
                var score = estimator.Estimate(state, player);
                return score;
            }

            if (activePlayer == player)
                depth--;

            var top = players[activePlayer].dir == UnsafePlayer.DIR_NULL ? 6 : 5;

            for (byte d = 3; d <= top; d++)
            {
                var action = (byte)((players[activePlayer].dir + d) % 4);
                var ne = UnsafeState.NextCoord(players[activePlayer].arrivePos, action);
                if (ne == ushort.MaxValue 
                    || (state->territory[ne] & UnsafeState.TERRITORY_LINE_MASK) == activePlayer << UnsafeState.TERRITORY_LINE_SHIFT)
                    continue;
                
                var nextPlayer = activePlayer == player ? 0 : activePlayer + 1;
                for (; nextPlayer < state->playersCount; nextPlayer++)
                {
                    if (nextPlayer == player
                        || players[nextPlayer].status == UnsafePlayer.STATUS_ELIMINATED
                        || players[nextPlayer].status == UnsafePlayer.STATUS_BROKEN
                        || players[nextPlayer].arriveTime > 0)
                        continue;
                    break;
                }

                var prev = players[activePlayer].dir;
                players[activePlayer].dir = action;
                var moved = false;
                if (nextPlayer == state->playersCount)
                {
                    moved = true;
                    undo.Prepare(state);
                    state->NextTurn(capture, &undo);

                    if (players[player].status != UnsafePlayer.STATUS_ELIMINATED && players[player].arriveTime == 0)
                        nextPlayer = player;
                    else
                    {
                        for (nextPlayer = 0; nextPlayer < state->playersCount; nextPlayer++)
                        {
                            if (nextPlayer == player
                                || players[nextPlayer].status == UnsafePlayer.STATUS_ELIMINATED
                                || players[nextPlayer].status == UnsafePlayer.STATUS_BROKEN
                                || players[nextPlayer].arriveTime > 0)
                                continue;
                            break;
                        }
                    }
                }

                var score = Alphabeta(timeManager, state, player, depth, nextPlayer, a, b, out _, capture);

                if (moved)
                    undo.Undo(state);

                players[activePlayer].dir = prev;

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