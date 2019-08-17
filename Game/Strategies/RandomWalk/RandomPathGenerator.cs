using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
{
    public class RandomPathGenerator
    {
        private const int sameDirChance = 10;

        public Random random;
        private readonly Direction[] dirs;
        private readonly int[] dirChances;
        private int dirsCount;

        public RandomPathGenerator()
        {
            dirs = new Direction[4];
            dirChances = new int[4];
        }

        public bool Generate(State state, int player, DistanceMap distanceMap, ReliablePathBuilder builder, byte allowedDirectionsMask = 0xFF)
        {
            builder.Start(state, player, distanceMap);
            while (true)
            {
                if (builder.dir == null)
                {
                    dirs[0] = Direction.Up;
                    dirs[1] = Direction.Left;
                    dirs[2] = Direction.Right;
                    dirs[3] = Direction.Down;
                    dirChances[0] = 1;
                    dirChances[1] = 1;
                    dirChances[2] = 1;
                    dirChances[3] = 1;
                    dirsCount = 4;
                    random.Shuffle(dirs);
                }
                else
                {
                    dirs[0] = builder.dir.Value;
                    dirs[1] = (Direction)(((int)dirs[0] + 1) % 4);
                    dirs[2] = (Direction)(((int)dirs[0] + 3) % 4);
                    dirsCount = 3;

                    if (random.Next(2) == 0)
                    {
                        var tmp = dirs[1];
                        dirs[1] = dirs[2];
                        dirs[2] = tmp;
                    }

                    if (random.Next(sameDirChance) == 0)
                    {
                        var other = random.Next(1, 3);
                        var tmp = dirs[0];
                        dirs[0] = dirs[other];
                        dirs[other] = tmp;
                    }
                }

                var found = false;
                for (var i = 0; i < dirsCount; i++)
                {
                    var nextDir = dirs[i];
                    if (builder.len == 0 && (allowedDirectionsMask & (1 << (int)nextDir)) == 0)
                        continue;
                    
                    var nextPos = builder.pos.NextCoord(nextDir);
                    if (nextPos == ushort.MaxValue)
                        continue;

                    if (!builder.TryAdd(state, player, distanceMap, nextPos))
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                    return false;

                if (builder.started && state.territory[builder.pos] == player)
                    return true;
            }
        }
    }
}