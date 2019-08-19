using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Undo;
using Game.Strategies.RandomWalk;

namespace Game.Strategies.BruteForce
{
    public class Minimax
    {
        private readonly IMinimaxEstimator estimator;
        private readonly int maxDepth;

        public int estimations;
        public double bestScore;
        public int bestDepth;
        public Direction bestAction;
        public double[] bestResultScores;

        private double[] resultScores;
        private Direction[] commands;
        private bool[] skipPlayers;
        
        public Minimax(IMinimaxEstimator estimator, int maxDepth)
        {
            this.estimator = estimator;
            this.maxDepth = maxDepth;
            commands = new Direction[6 * maxDepth * 6];
            resultScores = new double[4];
            bestResultScores = new double[4];
        }

        public void Alphabeta(ITimeManager timeManager, State state, int player, PlayerPath[] skipPaths, DistanceMap distanceMap, InterestingFacts facts)
        {
            if (skipPaths != null && (skipPlayers == null || skipPlayers.Length < state.players.Length))
                skipPlayers = new bool[state.players.Length];

            bestScore = double.MinValue;
            bestDepth = 0;
            estimations = 0;
            bestAction = default(Direction);

            for (int i = 0; i < 4; i++)
                bestResultScores[i] = double.NegativeInfinity;

            var depth = 1;
            while (!timeManager.IsExpired && depth <= maxDepth)
            {
                for (int i = 0; i < 4; i++)
                    resultScores[i] = double.NegativeInfinity;

                if (skipPaths != null)
                {
                    for (int i = 0; i < state.players.Length; i++)
                    {
                        skipPlayers[i] = true;
                        if (i == player || state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                            continue;

                        var active = distanceMap.nearestOpponentActive[i, player];
                        if (active != ushort.MaxValue)
                        {
                            var dist = distanceMap.distances1[i, active];
                            if (dist != -1 && dist != int.MaxValue && dist <= 2 * depth)
                            {
                                skipPlayers[i] = false;
                                continue;
                            }
                            dist = distanceMap.distances2[i, active];
                            if (dist != -1 && dist != int.MaxValue && dist <= 2 * depth)
                            {
                                skipPlayers[i] = false;
                                continue;
                            }
                        }

                        if (facts.sawCollectDistance[i] != int.MaxValue)
                        {
                            if (facts.sawCollectDistance[i] <= depth)
                            {
                                skipPlayers[i] = false;
                                continue;
                            }
                        }
                    }
                }

                var score = Alphabeta(timeManager, state, player, depth, player, double.MinValue, double.MaxValue, resultScores, out var action, 0, skipPaths, facts);
                if (double.IsNegativeInfinity(score))
                    break;
                bestScore = score;
                bestAction = action;
                bestDepth = depth;
                for (int i = 0; i < 4; i++)
                    bestResultScores[i] = resultScores[i];
                depth++;
            }
        }

        private double Alphabeta(
            ITimeManager timeManager,
            State state,
            int player,
            int depth,
            int activePlayer,
            double a,
            double b,
            double[] rootScores,
            out Direction resultAction,
            int commandsStart,
            PlayerPath[] skipPaths,
            InterestingFacts facts)
        {
            resultAction = default(Direction);
            if (timeManager.IsExpired)
                return double.NegativeInfinity;

            if (state.isGameOver
                || state.players[player].status == PlayerStatus.Eliminated
                || depth == 0 && activePlayer == player)
            {
                estimations++;
                var score = estimator.Estimate(state, player, facts);
                return score;
            }

            if (activePlayer == player)
                depth--;

            var top = state.players[activePlayer].dir == null ? 6 : 5;

            var bestRootScore = double.MinValue;

            for (byte d = 3; d <= top; d++)
            {
                var action = (Direction)(((byte)(state.players[activePlayer].dir ?? Direction.Up) + d) % 4);
                var ne = state.players[activePlayer].arrivePos.NextCoord(action);
                if (ne == ushort.MaxValue || (state.lines[ne] & (1 << activePlayer)) != 0)
                    continue;

                ulong skippedMask = 0;
                commands[commandsStart + activePlayer] = action;
                var nextPlayer = activePlayer == player ? 0 : activePlayer + 1;
                for (; nextPlayer < state.players.Length; nextPlayer++)
                {
                    if (nextPlayer == player
                        || state.players[nextPlayer].status == PlayerStatus.Eliminated
                        || state.players[nextPlayer].status == PlayerStatus.Broken
                        || state.players[nextPlayer].arriveTime > 0)
                        continue;

                    if (skipPaths != null && skipPlayers[nextPlayer])
                    {
                        commands[commandsStart + nextPlayer] = skipPaths[nextPlayer].ApplyNext(state, nextPlayer);
                        skippedMask += 1ul << (nextPlayer * 8);
                        continue;
                    }

                    break;
                }

                StateUndo undo = null;
                if (nextPlayer == state.players.Length)
                {
                    while (true)
                    {
                        var nextUndo = state.NextTurn(commands, withUndo: true, commandsStart: commandsStart);
                        if (undo != null)
                            nextUndo.prevUndo = undo;
                        
                        undo = nextUndo;
                        commandsStart += 6;

                        if (state.isGameOver || state.players[player].status == PlayerStatus.Eliminated)
                        {
                            nextPlayer = state.players.Length;
                            break;
                        }

                        if (state.players[player].arriveTime == 0)
                        {
                            nextPlayer = player;
                            break;
                        }

                        var done = false;
                        for (nextPlayer = 0; nextPlayer < state.players.Length; nextPlayer++)
                        {
                            if (nextPlayer == player
                                || state.players[nextPlayer].status == PlayerStatus.Eliminated
                                || state.players[nextPlayer].status == PlayerStatus.Broken
                                || state.players[nextPlayer].arriveTime > 0)
                                continue;

                            if (skipPaths != null && skipPlayers[nextPlayer])
                            {
                                commands[commandsStart + nextPlayer] = skipPaths[nextPlayer].ApplyNext(state, nextPlayer);
                                skippedMask += 1ul << (nextPlayer * 8);
                                continue;
                            }

                            done = true;
                            break;
                        }
                        
                        if (done)
                            break;
                    }
                }

                var score = Alphabeta(timeManager, state, player, depth, nextPlayer, a, b, null, out _, commandsStart, skipPaths, facts);

                while (undo != null)
                {
                    var prevUndo = undo.prevUndo;
                    state.Undo(undo);
                    undo = prevUndo;
                    commandsStart -= 6;
                }

                if (skippedMask != 0)
                {
                    for (int i = 0; i < state.players.Length; i++)
                    {
                        while ((skippedMask & (0xFFul << (i * 8))) != 0)
                        {
                            skippedMask -= 1ul << (i * 8);
                            skipPaths[i].Revert();
                        }
                    }
                }

                if (double.IsNegativeInfinity(score))
                    return double.NegativeInfinity;

                if (rootScores != null)
                {
                    rootScores[(int)action] = score;
                    if (score > bestRootScore)
                        bestRootScore = score;
                }
                else
                {
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
            }

            return rootScores != null ? bestRootScore
                : player == activePlayer ? a : b;
        }
    }
}