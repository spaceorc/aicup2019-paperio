using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class RandomPathGenerator
    {
        private const int sameDirChance = 10;
        
        public Random random;

        public ushort[] coords;
        public int[] times;
        public int[] used;
        public int len;
        public int gen;
        
        public Direction[] dirs;
        public int[] dirChances;
        public int dirsCount;

        public bool Generate(FastState state, int player, DistanceMapGenerator distanceMap)
        {
            if (coords == null)
            {
                coords = new ushort[state.config.x_cells_count * state.config.y_cells_count];
                times = new int[coords.Length];
                dirs = new Direction[4];
                dirChances = new int[4];
                used = new int[state.config.x_cells_count * state.config.y_cells_count];
            }

            gen++;
            len = 0;
            var timeLimit = Env.MAX_TICK_COUNT - state.time; 
            for (int i = 0; i < state.players[player].lineCount; i++)
            {
                var line = state.players[player].line[i];
                used[line] = gen;
                for (int other = 0; other < state.players.Length; other++)
                {
                    if (other == player)
                        continue;
                    if (state.players[other].status == PlayerStatus.Eliminated)
                        continue;
                    if (distanceMap.times[other, line] != -1 && distanceMap.times[other, line] < timeLimit)
                        timeLimit = distanceMap.times[other, line];

                    if (distanceMap.nearestOwned[other] != ushort.MaxValue)
                    {
                        var timeToOwn = distanceMap.times[other, distanceMap.nearestOwned[other]];
                        if (timeToOwn != -1 && timeToOwn != int.MaxValue)
                        {
                            // todo учесть бонусы врага 
                            var timeToOur = timeToOwn + state.MDist(distanceMap.nearestOwned[other], line) * state.players[other].shiftTime;
                            if (timeToOur < timeLimit)
                                timeLimit = timeToOur;
                        }
                    }
                }
            }

            var pos = state.players[player].arrivePos;
            var dir = state.players[player].dir;
            var time = 0;
            var shiftTime = state.players[player].shiftTime;
            var nitroLeft = state.players[player].nitroLeft;
            var slowLeft = state.players[player].slowLeft;
            while (true)
            {
                if (dir == null)
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
                    dirs[0] = dir.Value;
                    dirs[1] = (Direction)(((int)dir.Value + 1) % 4);
                    dirs[2] = (Direction)(((int)dir.Value + 3) % 4);
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
                for (int i = 0; i < dirsCount; i++)
                {
                    var nextDir = dirs[i];
                    var nextPos = state.NextCoord(pos, nextDir);
                    if (nextPos == ushort.MaxValue)
                        continue;

                    if (pos == state.players[player].arrivePos)
                    {
                        if (state.territory[pos] == player && state.territory[nextPos] == player)
                            continue;
                    }

                    if (used[nextPos] == gen)
                        continue;

                    var nextTime = time + shiftTime;
                    if (nextTime > timeLimit || nextTime == timeLimit && state.territory[nextPos] != player)
                        continue;

                    var nextNitroLeft = nitroLeft; 
                    var nextSlowLeft = slowLeft; 
                    if (nextNitroLeft > 0)
                        nextNitroLeft--;
                    if (nextSlowLeft > 0)
                        nextSlowLeft--;
                    for (int b = 0; b < state.bonusCount; b++)
                    {
                        if (state.bonuses[b].pos == nextPos)
                        {
                            if (state.bonuses[b].type == BonusType.S)
                                nextSlowLeft += 50;
                            else if (state.bonuses[b].type == BonusType.N)
                                nextNitroLeft += 10;
                        }
                    }
                    var nextShiftTime = FastPlayer.GetShiftTime(state.config, nextNitroLeft, nextSlowLeft);
                    var escapeTime = nextTime + nextShiftTime;

                    var nextTimeLimit = timeLimit;
                    for (int other = 0; other < state.players.Length; other++)
                    {
                        if (other == player)
                            continue;
                        if (state.players[other].status == PlayerStatus.Eliminated)
                            continue;
                        var otherTimeToPos = distanceMap.times[other, nextPos];
                        if (otherTimeToPos != -1 && otherTimeToPos != int.MaxValue)
                        {
                            if (otherTimeToPos < nextTimeLimit)
                                nextTimeLimit = otherTimeToPos;
                            
                            if (otherTimeToPos == 0)
                            {
                                nextTimeLimit = -1;
                                break;
                            }
                            
                            var prevOtherPos = distanceMap.paths[other, nextPos];
                            var prevShiftTime = FastPlayer.GetShiftTime(state.config, distanceMap.nitroLefts[other, prevOtherPos], distanceMap.slowLefts[other, prevOtherPos]);
                            var otherEnterTime = otherTimeToPos - prevShiftTime;

                            if (otherEnterTime < escapeTime)
                            {
                                nextTimeLimit = -1;
                                break;
                            }
                        }
                    }
                    if (nextTime > nextTimeLimit || nextTime == nextTimeLimit && state.territory[nextPos] != player)
                        continue;

                    found = true;
                    coords[len] = nextPos;
                    times[len] = nextTime;
                    used[nextPos] = gen;
                    len++;
                    dir = nextDir;
                    pos = nextPos;
                    time = nextTime;
                    timeLimit = nextTimeLimit;
                    nitroLeft = nextNitroLeft;
                    slowLeft = nextSlowLeft;
                    shiftTime = nextShiftTime;
                    break;
                }

                if (!found)
                    return false;

                if (state.territory[pos] == player)
                    return true;
            }
        }
    }
}