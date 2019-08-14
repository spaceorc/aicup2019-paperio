using System;
using Game.Helpers;
using Game.Protocol;
using Game.RandomWalk.PathEstimators;
using Game.RandomWalk.StartPathStrategies;
using Game.Sim;
using Game.Sim.Undo;
using Game.Strategies;

namespace Game.RandomWalk
{
    public class RandomWalkAi : IAi
    {
        private readonly IStartPathStrategy startPathStrategy;
        private readonly IPathEstimator estimator;
        private readonly RandomPathGenerator randomPath;
        private readonly DistanceMapGenerator distanceMap = new DistanceMapGenerator();
        private readonly StateBackup backup = new StateBackup();
        private PathBuilder[] paths;
        private Direction[] commands;

        public RandomWalkAi(IStartPathStrategy startPathStrategy, IPathEstimator estimator, bool walkOnTerritory)
        {
            this.startPathStrategy = startPathStrategy;
            this.estimator = estimator;
            randomPath = new RandomPathGenerator(walkOnTerritory);
        }

        public RequestOutput GetCommand(State state, int player, ITimeManager timeManager, Random random)
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

            if (TryKillOpponent(state, player, out var kill))
                return kill;

            if (!randomPath.walkOnTerritory && TryGotoStart(state, player, out var gotoStart))
                return gotoStart;

            backup.Backup(state);
            estimator.Before(state, player);
            var invalidPathCounter = 0;
            var pathCounter = 0;
            var validPathCounter = 0;
            Direction? bestDir = null;
            double bestScore = 0;
            var bestLen = 0;
            string bestPath = null;
            long simulations = 0;
            while (!timeManager.IsExpired)
            {
                ++pathCounter;
                if (randomPath.Generate(state, player, distanceMap))
                {
                    ++validPathCounter;
                    var dir = default(Direction);
                    for (var i = 0; i < state.players.Length; i++)
                    {
                        if (i == player)
                        {
                            paths[i].BuildPath(state, randomPath, i);
                            dir = paths[i].dirs[paths[i].len - 1];
                        }
                        else
                        {
                            if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                                paths[i].Clear();
                            else
                                paths[i].BuildPath(state, distanceMap, i, distanceMap.nearestOwned[i]);
                        }
                    }

                    while (true)
                    {
                        for (var i = 0; i < state.players.Length; i++)
                        {
                            if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                                continue;
                            if (state.players[i].arriveTime != 0)
                                continue;

                            if (paths[i].len > 0)
                            {
                                commands[i] = paths[i].dirs[paths[i].len-- - 1];
                            }
                            else if (state.players[i].dir != null)
                            {
                                for (var d = 3; d <= 5; d++)
                                {
                                    var nd = (Direction)(((int)state.players[i].dir.Value + d) % 4);
                                    var next = state.players[i].arrivePos.NextCoord(nd);
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
                            var score = estimator.Estimate(state, player, randomPath.startLen);
                            if (score > bestScore || score > bestScore - 1e-6 && randomPath.len < bestLen)
                            {
                                bestScore = score;
                                bestDir = dir;
                                bestLen = randomPath.len;
                                bestPath = paths[player].Print();
                                Logger.Debug($"Score: {bestScore}; Path: {bestPath}");
                                if (Logger.IsEnabled(Logger.Level.Debug))
                                {
                                    for (var i = 0; i < paths.Length; i++)
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
                if (randomPath.walkOnTerritory && TryGotoStart(state, player, out gotoStart))
                    return gotoStart;
                
                paths[player].BuildPath(state, distanceMap, player, distanceMap.nearestOwned[player]);
                if (paths[player].len > 0)
                    return new RequestOutput {Command = paths[player].dirs[paths[player].len - 1], Debug = $"No path found. Returning back to territory. Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}"};

                Direction? validDir = null;
                if (state.players[player].dir == null)
                {
                    for (var d = 0; d < 4; d++)
                    {
                        var next = state.players[player].arrivePos.NextCoord((Direction)d);
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
                    var sd = random.Next(3);
                    for (var d = 0; d < 4; d++)
                    {
                        var nd = (Direction)(((int)state.players[player].dir.Value + 3 + sd + d) % 4);
                        if (nd == (Direction)(((int)state.players[player].dir.Value + 2) % 4))
                            continue;
                        var next = state.players[player].arrivePos.NextCoord(nd);
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

            return new RequestOutput {Command = bestDir, Debug = $"Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. BestLen: {bestLen}. BestPath: {bestPath}. BestScore: {bestScore}"};
        }

        private bool TryGotoStart(State state, int player, out RequestOutput result)
        {
            if (state.territory[state.players[player].arrivePos] == player)
            {
                var startPathOutput = startPathStrategy.GotoStart(state, player, distanceMap);
                if (startPathOutput != null)
                {
                    result = startPathOutput;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryKillOpponent(State state, int player, out RequestOutput result)
        {
            if (state.territory[state.players[player].arrivePos] == player)
            {
                if (state.players[player].dir != null)
                {
                    for (var d = 3; d <= 5; d++)
                    {
                        var nd = (Direction)(((int)state.players[player].dir.Value + d) % 4);
                        var ne = state.players[player].arrivePos.NextCoord(nd);
                        if (ne != ushort.MaxValue)
                        {
                            if (state.territory[ne] == player)
                            {
                                for (var other = 0; other < state.players.Length; other++)
                                {
                                    if (other == player || state.players[other].status == PlayerStatus.Eliminated)
                                        continue;
                                    if ((state.lines[ne] & (1 << other)) != 0)
                                    {
                                        if (distanceMap.nearestOwned[other] != ushort.MaxValue)
                                        {
                                            var timeToOwn = distanceMap.times[other, distanceMap.nearestOwned[other]];
                                            var timeToCatch = state.players[player].shiftTime;
                                            if (timeToCatch < timeToOwn)
                                            {
                                                result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha line! {state.players[player].arrivePos}->{ne}"};
                                                return true;
                                            }
                                        }
                                        else
                                        {
                                            result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha line! {state.players[player].arrivePos}->{ne}"};
                                            return true;
                                        }
                                    }

                                    if (state.players[other].arrivePos == ne && state.players[other].lineCount > 0)
                                    {
                                        if (state.players[other].arriveTime > 0)
                                        {
                                            result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha arriving player! {state.players[player].arrivePos}->{ne}"};
                                            return true;
                                        }

                                        if (state.players[other].dir != null)
                                        {
                                            if (ne.NextCoord(state.players[other].dir.Value) == state.players[player].arrivePos)
                                            {
                                                result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha arrived player! {state.players[player].arrivePos}->{ne}"};
                                                return true;
                                            }

                                            if (state.players[other].shiftTime > state.players[player].shiftTime)
                                            {
                                                result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha slow arrived player! {state.players[player].arrivePos}->{ne}"};
                                                return true;
                                            }
                                        }
                                    }

                                    if (state.players[other].pos == ne && state.players[other].lineCount > 0 && state.players[other].arriveTime > 1)
                                    {
                                        if (state.players[other].dir != null)
                                        {
                                            if (state.players[player].arrivePos.NextCoord(state.players[other].dir.Value) != ne)
                                            {
                                                result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha escaping player! {state.players[player].arrivePos}->{ne}"};
                                                return true;
                                            }

                                            if (state.players[player].shiftTime < state.players[other].arriveTime)
                                            {
                                                result = new RequestOutput {Command = state.players[player].arrivePos.DirTo(ne), Debug = $"Gotcha slow escaping player! {state.players[player].arrivePos}->{ne}"};
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            result = null;
            return false;
        }
    }
}