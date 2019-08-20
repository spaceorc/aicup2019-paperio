using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Undo;
using Game.Strategies.BruteForce;
using Game.Strategies.RandomWalk.PathEstimators;
using Game.Strategies.RandomWalk.StartPathStrategies;

namespace Game.Strategies.RandomWalk
{
    public class RandomWalkAi : IAi
    {
        private readonly IStartPathStrategy startPathStrategy;
        private readonly IPathEstimator estimator;
        private readonly bool useAllowedDirections;
        private readonly RandomPathGenerator randomPath;
        private readonly ReliablePathBuilder reliablePathBuilder;
        private readonly DistanceMap distanceMap = new DistanceMap();
        private readonly InterestingFacts facts = new InterestingFacts();
        private readonly StateBackup backup = new StateBackup();
        private readonly AllowedDirectionsFinder allowedDirectionsFinder;
        private Direction[] commands;

        public RandomWalkAi()
            : this(new NearestOpponentStartPathStrategy(), new CaptureOpponentEstimator(150), true, true, false)
        {
        }

        public RandomWalkAi(
            IStartPathStrategy startPathStrategy,
            IPathEstimator estimator,
            bool useAllowedDirections,
            bool useTerritoryTtl,
            bool killWithMinimax)
        {
            this.startPathStrategy = startPathStrategy;
            this.estimator = estimator;
            this.useAllowedDirections = useAllowedDirections;
            randomPath = new RandomPathGenerator();
            reliablePathBuilder = new ReliablePathBuilder(useTerritoryTtl);
            allowedDirectionsFinder = new AllowedDirectionsFinder(4, killWithMinimax);
        }

        public RequestOutput GetCommand(State state, int player, ITimeManager timeManager, Random random)
        {
            if (commands == null)
                commands = new Direction[state.players.Length];

            randomPath.random = random;

            distanceMap.Build(state);
            facts.Build(state, distanceMap);

            if (TryKillOpponent(state, player, out var kill))
                return kill;

            backup.Backup(state);
            estimator.Before(state, player);

            var allowedDirectionsMask = useAllowedDirections
                ? allowedDirectionsFinder.GetAllowedDirectionsMask(timeManager.GetNested(70), state, player, distanceMap, facts)
                : (byte)0xFF;

            if (allowedDirectionsMask != 0 && (allowedDirectionsMask & (allowedDirectionsMask - 1)) == 0)
            {
                for (int d = 0; d < 4; d++)
                {
                    if ((allowedDirectionsMask & (1 << d)) != 0)
                        return new RequestOutput {Command = (Direction)d, Debug = $"Only one allowed direction. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
                }
            }

            if (allowedDirectionsFinder.killWithMinimax)
            {
                var bestKillScore = 0.0;
                Direction? bestKillCommand = null;
                for (int d = 0; d < 4; d++)
                {
                    if ((allowedDirectionsMask & (1 << d)) != 0 && allowedDirectionsFinder.minimax.bestResultScores[d] > bestKillScore)
                    {
                        bestKillScore = allowedDirectionsFinder.minimax.bestResultScores[d];
                        bestKillCommand = (Direction)d;
                    }
                }

                if (bestKillScore > AllowedDirectionsFinder.killScore)
                    return new RequestOutput {Command = bestKillCommand, Debug = $"Gotcha with minimax! AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
            }

            var pathCounter = 0;
            var validPathCounter = 0;
            Direction? bestDir = null;
            double bestScore = 0;
            var bestLen = 0;
            string bestPath = null;
            long simulations = 0;
            long bestPathCounter = 0;
            var opponentCapturedFound = false;

            while (allowedDirectionsMask != 0 && !timeManager.IsExpired)
            {
                ++pathCounter;
                if (randomPath.Generate(state, player, distanceMap, facts, reliablePathBuilder, allowedDirectionsMask))
                {
                    ++validPathCounter;
                    var dir = default(Direction);
                    for (var i = 0; i < state.players.Length; i++)
                    {
                        if (i != player)
                            facts.pathsToOwned[i].Reset();
                        else
                        {
                            facts.pathsToOwned[i].BuildPath(state, reliablePathBuilder, i);
                            dir = facts.pathsToOwned[i].CurrentAction();
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

                            commands[i] = facts.pathsToOwned[i].ApplyNext(state, i);
                        }

                        state.NextTurn(commands, false);
                        simulations++;
                        if (state.isGameOver || state.players[player].status == PlayerStatus.Eliminated || facts.pathsToOwned[player].len == 0 && state.players[player].arriveTime == 0)
                        {
                            if (state.players[player].status != PlayerStatus.Eliminated)
                            {
                                if (!opponentCapturedFound && state.players[player].opponentTerritoryCaptured > 0)
                                    opponentCapturedFound = true;

                                if (!opponentCapturedFound || state.players[player].opponentTerritoryCaptured > 0)
                                {
                                    var score = estimator.Estimate(state, facts, player, reliablePathBuilder.startLen);
                                    if (score > bestScore || score > bestScore - 1e-6 && reliablePathBuilder.len < bestLen)
                                    {
                                        bestScore = score;
                                        bestDir = dir;
                                        bestLen = reliablePathBuilder.len;
                                        bestPath = facts.pathsToOwned[player].Print();
                                        bestPathCounter = pathCounter;
                                        Logger.Debug($"Score: {bestScore}; Path: {bestPath}");
                                        if (Logger.IsEnabled(Logger.Level.Debug))
                                        {
                                            for (var i = 0; i < state.players.Length; i++)
                                            {
                                                Logger.Debug($"{i}: {facts.pathsToOwned[i].Print()} {state.players[i].status}");
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    }

                    backup.Restore(state);
                }
            }

            if (bestDir == null)
            {
                if (TryGotoStart(state, player, allowedDirectionsMask, out var gotoStart))
                    return gotoStart;

                facts.pathsToOwned[player].BuildPath(state, distanceMap, player, distanceMap.nearestOwned[player]);
                if (facts.pathsToOwned[player].len > 0)
                {
                    var returnAction = facts.pathsToOwned[player].CurrentAction();
                    if ((allowedDirectionsMask & (1 << (int)returnAction)) != 0 || allowedDirectionsMask == 0)
                        return new RequestOutput {Command = returnAction, Debug = $"No path found. Returning back to territory. Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
                }

                Direction? validDir = null;
                if (state.players[player].dir == null)
                {
                    for (var d = 0; d < 4; d++)
                    {
                        if ((allowedDirectionsMask & (1 << d)) != 0)
                        {
                            var next = state.players[player].arrivePos.NextCoord((Direction)d);
                            if (next != ushort.MaxValue)
                            {
                                validDir = (Direction)d;
                                if (state.territory[next] == player)
                                    return new RequestOutput {Command = (Direction)d, Debug = $"No path found. Walking around (null). Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
                            }
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
                        if ((allowedDirectionsMask & (1 << (int)nd)) != 0)
                        {
                            var next = state.players[player].arrivePos.NextCoord(nd);
                            if (next != ushort.MaxValue)
                            {
                                validDir = nd;
                                if (state.territory[next] == player)
                                    return new RequestOutput {Command = nd, Debug = $"No path found. Walking around. Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
                            }
                        }
                    }
                }

                return new RequestOutput {Command = validDir ?? throw new InvalidOperationException("validDir is null"), Debug = $"No path found. Walking around (not self). Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
            }

            return new RequestOutput {Command = bestDir ?? throw new InvalidOperationException("bestDir is null"), Debug = $"Paths: {pathCounter}. ValidPaths: {validPathCounter}. Simulations: {simulations}. BestLen: {bestLen}. BestPath: {bestPath}. BestScore: {bestScore}. BestPathCounter: {bestPathCounter}. AllowedDirections: {AllowedDirectionsFinder.DescribeAllowedDirectionsMask(allowedDirectionsMask)} (depth {allowedDirectionsFinder.minimax.bestDepth})"};
        }

        private bool TryGotoStart(State state, int player, byte allowedDirectionsMask, out RequestOutput result)
        {
            if (state.territory[state.players[player].arrivePos] == player)
            {
                var startPathOutput = startPathStrategy.GotoStart(state, player, allowedDirectionsMask, distanceMap);
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
                                            var timeToOwn = distanceMap.times1[other, distanceMap.nearestOwned[other]];
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