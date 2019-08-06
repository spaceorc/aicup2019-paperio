using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class RandomWalkAi : IAi
    {
        private readonly bool conquerOpponent;
        private readonly RandomPathGenerator randomPath = new RandomPathGenerator();
        private readonly DistanceMapGenerator distanceMap = new DistanceMapGenerator();
        private readonly FastStateBackup backup = new FastStateBackup();
        private PathBuilder[] paths;
        private Direction[] commands;

        public RandomWalkAi(bool conquerOpponent)
        {
            this.conquerOpponent = conquerOpponent;
        }

        public RequestOutput GetCommand(FastState state, int player, ITimeManager timeManager, Random random)
        {
            if (paths == null)
            {
                paths = new PathBuilder[state.players.Length];
                commands = new Direction[state.players.Length];
                for (var i = 0; i < state.players.Length; i++)
                    paths[i] = new PathBuilder();
            }

            randomPath.random = random;

            distanceMap.Build(state);

            if (conquerOpponent)
            {
                if (state.territory[state.players[player].arrivePos] == player)
                {
                    var target = distanceMap.nearestOpponent[player];
                    if (target == ushort.MaxValue)
                    {
                        target = distanceMap.nearestEmpty[player];
                        if (target == ushort.MaxValue)
                            throw new InvalidOperationException("Couldn't find nearest to conquer");
                    }

                    var next = target;
                    for (var cur = target; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                        next = cur;

                    if (next != target && state.territory[next] == player)
                        return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, next), Debug = $"Goto nearest {state.players[player].arrivePos}->{target}"};
                }
            }
            else
            {
                if (state.territory[state.players[player].arrivePos] == player)
                {
                    var empty = distanceMap.nearestEmpty[player];
                    if (empty == ushort.MaxValue)
                        throw new InvalidOperationException("Couldn't find nearest to conquer");

                    var next = empty;
                    for (var cur = empty; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                        next = cur;

                    if (next != empty)
                        return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, next), Debug = $"Goto nearest {state.players[player].arrivePos}->{empty}"};
                }
            }

            backup.Backup(state);
            var invalidPathCounter = 0;
            var pathCounter = 0;
            var validPathCounter = 0;
            Direction? bestDir = null;
            int bestScore = 0;
            int bestLen = 0;
            long simulations = 0;
            while (!timeManager.IsExpired)
            {
                ++pathCounter;
                if (randomPath.Generate(state, player, distanceMap))
                {
                    ++validPathCounter;
                    var dir = default(Direction);
                    for (int i = 0; i < state.players.Length; i++)
                    {
                        if (i == player)
                        {
                            paths[i].BuildPath(state, randomPath, i);
                            dir = paths[i].dirs[paths[i].len - 1];
                        }
                        else
                        {
                            if (state.players[i].status != PlayerStatus.Eliminated)
                                paths[i].BuildPath(state, distanceMap, i, distanceMap.nearestOwned[i]);
                            else
                                paths[i].Clear();
                        }
                    }

                    while (true)
                    {
                        for (int i = 0; i < state.players.Length; i++)
                        {
                            if (state.players[i].status == PlayerStatus.Eliminated)
                                continue;
                            if (state.players[i].arriveTime != 0)
                                continue;

                            if (paths[i].len > 0)
                            {
                                commands[i] = paths[i].dirs[paths[i].len-- - 1];
                            }
                            else if (state.players[i].dir != null)
                            {
                                for (int d = 3; d <= 5; d++)
                                {
                                    var nd = (Direction)(((int)state.players[i].dir.Value + d) % 4);
                                    var next = state.NextCoord(state.players[i].arrivePos, nd);
                                    if (next != ushort.MaxValue)
                                    {
                                        commands[i] = nd;
                                        if (state.territory[next] == i)
                                            break;
                                    }
                                }
                            }
                        }

                        state.NextTurn(commands, false);
                        simulations++;
                        if (state.isGameOver || state.players[player].status == PlayerStatus.Eliminated || paths[player].len == 0 && state.players[player].arriveTime == 0)
                        {
                            var score = Evaluate(state, player);
                            if (score > bestScore || score == bestScore && randomPath.len < bestLen)
                            {
                                bestScore = score;
                                bestDir = dir;
                                bestLen = randomPath.len;
                                Logger.Debug($"Score: {bestScore}; Path: {randomPath.Print(state, player)}");
                                if (Logger.IsEnabled(Logger.Level.Debug))
                                {
                                    for (int i = 0; i < paths.Length; i++)
                                    {
                                        Logger.Debug($"{i}: {paths[i].Print()} {state.players[i].status}");
                                    }
                                }
                            }

                            break;
                        }
                    }

                    backup.Restore(state);
                }
                else
                    invalidPathCounter++;
            }

            if (bestDir == null)
            {
                paths[player].BuildPath(state, distanceMap, player, distanceMap.nearestOwned[player]);
                if (paths[player].len > 0)
                    return new RequestOutput {Command = paths[player].dirs[paths[player].len - 1], Debug = $"No path found. Returning back to territory. Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}"};

                Direction? validDir = null;
                if (state.players[player].dir == null)
                {
                    for (int d = 0; d < 4; d++)
                    {
                        var next = state.NextCoord(state.players[player].arrivePos, (Direction)d);
                        if (next != ushort.MaxValue)
                        {
                            validDir = (Direction)d;
                            if (state.territory[next] == player)
                                return new RequestOutput {Command = (Direction)d, Debug = $"No path found. Walking around (null). Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}"};
                        }
                    }
                }
                else
                {
                    for (int d = 3; d <= 5; d++)
                    {
                        var nd = (Direction)(((int)state.players[player].dir.Value + d) % 4);
                        var next = state.NextCoord(state.players[player].arrivePos, nd);
                        if (next != ushort.MaxValue)
                        {
                            validDir = nd;
                            if (state.territory[next] == player)
                                return new RequestOutput {Command = nd, Debug = $"No path found. Walking around. Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}"};
                        }
                    }
                }

                return new RequestOutput {Command = validDir, Debug = $"No path found. Walking around (not self). Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}"};
            }

            return new RequestOutput {Command = bestDir ?? throw new InvalidOperationException("Couldn't best path"), Debug = $"Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. BestLen: {bestLen}. BestScore: {bestScore}"};
        }

        private int Evaluate(FastState state, int player)
        {
            var score = 0;
            if (state.players[player].status == PlayerStatus.Eliminated)
                score -= 1_000_000_000;
            else
            {
                for (int i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated && (state.players[i].killedBy & (1 << player)) != 0)
                        score += 1_000_000;
                }
            }

            score += state.players[player].score;
            return score;
        }
    }
}