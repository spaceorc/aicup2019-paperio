using System;
using Game.Fast;
using Game.Helpers;
using Game.Types;

namespace Game.Strategies
{
    public class SimpleAi : IAi
    {
        private readonly RandomPathGenerator randomPath = new RandomPathGenerator();
        private readonly DistanceMapGenerator distanceMap = new DistanceMapGenerator();
        private readonly FastStateBackup backup = new FastStateBackup();
        private PathBuilder[] paths;
        private Direction[] commands;

        public Direction? GetCommand(FastState state, int player, ITimeManager timeManager, Random random)
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
                var empty = distanceMap.nearestEmpty[player];
                if (empty == ushort.MaxValue)
                    return null;

                var next = empty;
                for (var cur = empty; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                    next = cur;

                if (next != empty)
                    return state.MakeDir(state.players[player].arrivePos, next);
            }

            backup.Backup(state);
            var invalidPathCounter = 0;
            var pathCounter = 0;
            Direction? bestDir = null;
            int bestScore = 0;
            while (!timeManager.IsExpired)
            {
                ++pathCounter;
                if (randomPath.Generate(state, player, distanceMap))
                {
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
                                commands[i] = paths[i].dirs[paths[i].len-- - 1];
                            else if (state.players[i].dir != null)
                            {
                                for (int d = 1; d <= 3; d++)
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
                        if (state.isGameOver || state.players[player].status == PlayerStatus.Eliminated || paths[player].len == 0 && state.players[player].arriveTime == 0)
                        {
                            var score = Evaluate(state, player);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestDir = dir;
                            }

                            break;
                        }
                    }

                    backup.Restore(state);
                }
                else
                {
                    if (bestDir == null)
                    {
                        invalidPathCounter++;
                        if (invalidPathCounter == 100)
                        {
                            paths[player].BuildPath(state, distanceMap, player, distanceMap.nearestOwned[player]);
                            if (paths[player].len > 0)
                                return paths[player].dirs[paths[player].len - 1];

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
                                            return (Direction)d;
                                    }
                                }
                            }
                            else
                            {
                                for (int d = 1; d <= 3; d++)
                                {
                                    var nd = (Direction)(((int)state.players[player].dir.Value + d) % 4);
                                    var next = state.NextCoord(state.players[player].arrivePos, nd);
                                    if (next != ushort.MaxValue)
                                    {
                                        validDir = nd;
                                        if (state.territory[next] == player)
                                            return nd;
                                    }
                                }
                            }

                            return validDir;
                        }
                    }
                }
            }

            return bestDir;
        }

        private int Evaluate(FastState state, int player)
        {
            var score = 0;
            if (state.players[player].status == PlayerStatus.Eliminated)
                score -= 1_000_000_000;
            else
                score += 10_000_000 - state.playersLeft * 1_000_000;

            score += state.players[player].score;
            return score;
        }
    }
}