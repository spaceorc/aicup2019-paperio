using System;
using System.Text;
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

                    var nearestOwned = distanceMap.nearestOwned[other];
                    if (nearestOwned != ushort.MaxValue)
                    {
                        var timeToOwn = distanceMap.times[other, nearestOwned];
                        if (timeToOwn != 0 && timeToOwn != -1 && timeToOwn != int.MaxValue)
                        {
                            var otherNitroLeft = distanceMap.nitroLefts[other, nearestOwned];
                            var otherSlowLeft = distanceMap.slowLefts[other, nearestOwned];
                            var mDist = state.MDist(nearestOwned, line);

                            if (otherNitroLeft > mDist)
                                otherNitroLeft = mDist;
                            if (otherSlowLeft > mDist)
                                otherSlowLeft = mDist;

                            var timeToOur = timeToOwn;
                            if (otherNitroLeft > otherSlowLeft)
                            {
                                mDist -= otherSlowLeft;
                                otherNitroLeft -= otherSlowLeft;
                                timeToOur += otherSlowLeft * state.config.ticksPerRequest;

                                mDist -= otherNitroLeft;
                                timeToOur += otherNitroLeft * state.config.nitroTicksPerRequest;

                                timeToOur += mDist * state.config.ticksPerRequest;
                            }
                            else
                            {
                                mDist -= otherNitroLeft;
                                otherSlowLeft -= otherNitroLeft;
                                timeToOur += otherNitroLeft * state.config.ticksPerRequest;

                                mDist -= otherSlowLeft;
                                timeToOur += otherSlowLeft * state.config.slowTicksPerRequest;

                                timeToOur += mDist * state.config.ticksPerRequest;
                            }

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

                    if (nextDir == Direction.Down && dir == Direction.Right && len == 1)
                    {
                        var a = 10;
                    }

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

                            if (state.players[other].arrivePos == nextPos)
                            {
                                nextTimeLimit = -1;
                                break;
                            }

                            if (state.territory[nextPos] != player)
                            {
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

                        var nearestOwned = distanceMap.nearestOwned[other];
                        if (nearestOwned != ushort.MaxValue)
                        {
                            var timeToOwn = distanceMap.times[other, nearestOwned];
                            if (timeToOwn != 0 && timeToOwn != -1 && timeToOwn != int.MaxValue)
                            {
                                var otherNitroLeft = distanceMap.nitroLefts[other, nearestOwned];
                                var otherSlowLeft = distanceMap.slowLefts[other, nearestOwned];
                                var mDist = state.MDist(nearestOwned, nextPos);

                                if (otherNitroLeft > mDist)
                                    otherNitroLeft = mDist;
                                if (otherSlowLeft > mDist)
                                    otherSlowLeft = mDist;

                                var prevShiftTime = otherNitroLeft == mDist && otherSlowLeft == mDist ? state.config.ticksPerRequest
                                    : otherNitroLeft == mDist ? state.config.nitroTicksPerRequest
                                    : otherSlowLeft == mDist ? state.config.slowTicksPerRequest
                                    : state.config.ticksPerRequest;
                                
                                var timeToOur = timeToOwn;
                                if (otherNitroLeft > otherSlowLeft)
                                {
                                    mDist -= otherSlowLeft;
                                    otherNitroLeft -= otherSlowLeft;
                                    timeToOur += otherSlowLeft * state.config.ticksPerRequest;

                                    mDist -= otherNitroLeft;
                                    timeToOur += otherNitroLeft * state.config.nitroTicksPerRequest;

                                    timeToOur += mDist * state.config.ticksPerRequest;
                                }
                                else
                                {
                                    mDist -= otherNitroLeft;
                                    otherSlowLeft -= otherNitroLeft;
                                    timeToOur += otherNitroLeft * state.config.ticksPerRequest;

                                    mDist -= otherSlowLeft;
                                    timeToOur += otherSlowLeft * state.config.slowTicksPerRequest;

                                    timeToOur += mDist * state.config.ticksPerRequest;
                                }

                                if (timeToOur < nextTimeLimit)
                                    nextTimeLimit = timeToOur;
                                
                                var otherEnterTime = timeToOur - prevShiftTime;
                                if (otherEnterTime < escapeTime)
                                {
                                    nextTimeLimit = -1;
                                    break;
                                }
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

        public string Print(FastState state, int player)
        {
            if (len == 0)
                return "<EMPTY>";

            var result = new StringBuilder();
            result.Append(state.ToV(coords[0]));
            for (int i = 1; i < len; i++)
            {
                result.Append("->");
                result.Append(state.ToV(coords[i]));
            }

            return result.ToString();
        }
    }
}