using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class RandomWalkAi : IAi
    {
        private readonly IStartPathStrategy startPathStrategy;
        private readonly RandomPathGenerator randomPath = new RandomPathGenerator();
        private readonly DistanceMapGenerator distanceMap = new DistanceMapGenerator();
        private readonly FastStateBackup backup = new FastStateBackup();
        private PathBuilder[] paths;
        private Direction[] commands;

        public RandomWalkAi(IStartPathStrategy startPathStrategy)
        {
            this.startPathStrategy = startPathStrategy;
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

            if (state.territory[state.players[player].arrivePos] == player)
            {
                if (state.players[player].dir != null)
                {
                    for (int d = 3; d <= 5; d++)
                    {
                        var nd = (Direction)(((int)state.players[player].dir.Value + d) % 4);
                        var ne = state.NextCoord(state.players[player].arrivePos, nd);
                        if (ne != ushort.MaxValue)
                        {
                            if (state.territory[ne] == player)
                            {
                                for (int other = 0; other < state.players.Length; other++)
                                {
                                    if (other == player || state.players[other].status == PlayerStatus.Eliminated)
                                        continue;
                                    if ((state.lines[ne] & (1 << other)) != 0 || (state.players[other].arrivePos == ne && state.players[other].lineCount > 0))
                                    {
                                        if (distanceMap.nearestOwned[other] != ushort.MaxValue)
                                        {
                                            var timeToOwn = distanceMap.times[other, distanceMap.nearestOwned[other]];
                                            var timeToCatch = state.players[player].shiftTime;
                                            if (timeToCatch < timeToOwn)
                                                return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, ne), Debug = $"Gotcha! {state.players[player].arrivePos}->{ne}"};
                                        }
                                        else
                                            return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, ne), Debug = $"Gotcha! {state.players[player].arrivePos}->{ne}"};
                                    }
                                }
                            }
                        }
                    }
                }
                
                var startPathOutput = startPathStrategy.GotoStart(state, player, distanceMap);
                if (startPathOutput != null)
                    return startPathOutput;
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
                    var sd = random.Next(3);
                    for (int d = 0; d < 4; d++)
                    {
                        var nd = (Direction)(((int)state.players[player].dir.Value + 3 + sd + d) % 4);
                        if (nd == (Direction)(((int)state.players[player].dir.Value + 2) % 4))
                            continue;
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

            // if (state.time < Env.MAX_TICK_COUNT - 100)
            // {
            //     score += state.players[player].nitrosCollected * 20;
            //     score -= state.players[player].slowsCollected * 50;
            // }

            // score += state.players[player].nitrosCollected * 30 * (state.config.ticksPerRequest - state.config.nitroTicksPerRequest) / state.config.ticksPerRequest; 
            // score -= state.players[player].slowsCollected * 30 * (state.config.slowTicksPerRequest - state.config.ticksPerRequest) / state.config.ticksPerRequest; 

            return score;
        }
    }
}