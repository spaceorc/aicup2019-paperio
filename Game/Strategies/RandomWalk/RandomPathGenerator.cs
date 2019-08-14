using System;
using System.Text;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
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
        public int startLen;

        public Direction[] dirs;
        public int[] dirChances;
        public int dirsCount;

        public readonly bool walkOnTerritory;

        public RandomPathGenerator(bool walkOnTerritory)
        {
            this.walkOnTerritory = walkOnTerritory;
        }

        public bool Generate(State state, int player, DistanceMapGenerator distanceMap)
        {
            if (coords == null)
            {
                coords = new ushort[Env.CELLS_COUNT];
                times = new int[coords.Length];
                dirs = new Direction[4];
                dirChances = new int[4];
                used = new int[Env.CELLS_COUNT];
            }

            gen++;
            len = 0;
            startLen = 0;
            var timeLimit = Env.MAX_TICK_COUNT - state.time;
            for (var i = 0; i < state.players[player].lineCount; i++)
            {
                var line = state.players[player].line[i];
                used[line] = gen;
                for (var other = 0; other < state.players.Length; other++)
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
                            var mDist = Coords.MDist(nearestOwned, line);

                            if (otherNitroLeft > mDist)
                                otherNitroLeft = mDist;
                            if (otherSlowLeft > mDist)
                                otherSlowLeft = mDist;

                            var timeToOur = timeToOwn;
                            if (otherNitroLeft > otherSlowLeft)
                            {
                                mDist -= otherSlowLeft;
                                otherNitroLeft -= otherSlowLeft;
                                timeToOur += otherSlowLeft * Env.TICKS_PER_REQUEST;

                                mDist -= otherNitroLeft;
                                timeToOur += otherNitroLeft * Env.NITRO_TICKS_PER_REQUEST;

                                timeToOur += mDist * Env.TICKS_PER_REQUEST;
                            }
                            else
                            {
                                mDist -= otherNitroLeft;
                                otherSlowLeft -= otherNitroLeft;
                                timeToOur += otherNitroLeft * Env.TICKS_PER_REQUEST;

                                mDist -= otherSlowLeft;
                                timeToOur += otherSlowLeft * Env.SLOW_TICKS_PER_REQUEST;

                                timeToOur += mDist * Env.TICKS_PER_REQUEST;
                            }

                            if (timeToOur < timeLimit)
                                timeLimit = timeToOur;
                        }
                    }
                }
            }

            var pos = state.players[player].arrivePos;
            var started = state.players[player].lineCount > 0;
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
                for (var i = 0; i < dirsCount; i++)
                {
                    var nextDir = dirs[i];
                    var nextPos = pos.NextCoord(nextDir);
                    if (nextPos == ushort.MaxValue)
                        continue;

                    if (!walkOnTerritory)
                    {
                        if (pos == state.players[player].arrivePos)
                        {
                            if (state.territory[pos] == player && state.territory[nextPos] == player)
                                continue;
                        }
                    }

                    if (used[nextPos] == gen)
                        continue;

                    var nextTime = time + shiftTime;
                    if (nextTime > timeLimit || nextTime == timeLimit && (!started || state.territory[nextPos] != player))
                        continue;

                    var nextNitroLeft = nitroLeft;
                    var nextSlowLeft = slowLeft;
                    if (nextNitroLeft > 0)
                        nextNitroLeft--;
                    if (nextSlowLeft > 0)
                        nextSlowLeft--;
                    for (var b = 0; b < state.bonusCount; b++)
                    {
                        if (state.bonuses[b].pos == nextPos)
                        {
                            if (state.bonuses[b].type == BonusType.S)
                                nextSlowLeft += 50;
                            else if (state.bonuses[b].type == BonusType.N)
                                nextNitroLeft += 10;
                        }
                    }

                    var nextShiftTime = Player.GetShiftTime(nextNitroLeft, nextSlowLeft);
                    var escapeTime = nextTime + nextShiftTime;

                    var nextTimeLimit = timeLimit;
                    var nextStarted = started || state.territory[nextPos] != player;
                    if (nextStarted)
                    {
                        for (var other = 0; other < state.players.Length; other++)
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

                                var prevOtherPos = distanceMap.paths[other, nextPos];
                                var prevShiftTime = Player.GetShiftTime(distanceMap.nitroLefts[other, prevOtherPos], distanceMap.slowLefts[other, prevOtherPos]);
                                var otherEnterTime = otherTimeToPos - prevShiftTime;

                                if (otherEnterTime < escapeTime)
                                {
                                    nextTimeLimit = -1;
                                    break;
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
                                    var mDist = Coords.MDist(nearestOwned, nextPos);

                                    if (otherNitroLeft > mDist)
                                        otherNitroLeft = mDist;
                                    if (otherSlowLeft > mDist)
                                        otherSlowLeft = mDist;

                                    var prevShiftTime = otherNitroLeft == mDist && otherSlowLeft == mDist ? Env.TICKS_PER_REQUEST
                                        : otherNitroLeft == mDist ? Env.NITRO_TICKS_PER_REQUEST
                                        : otherSlowLeft == mDist ? Env.SLOW_TICKS_PER_REQUEST
                                        : Env.TICKS_PER_REQUEST;

                                    var timeToOur = timeToOwn;
                                    if (otherNitroLeft > otherSlowLeft)
                                    {
                                        mDist -= otherSlowLeft;
                                        otherNitroLeft -= otherSlowLeft;
                                        timeToOur += otherSlowLeft * Env.TICKS_PER_REQUEST;

                                        mDist -= otherNitroLeft;
                                        timeToOur += otherNitroLeft * Env.NITRO_TICKS_PER_REQUEST;

                                        timeToOur += mDist * Env.TICKS_PER_REQUEST;
                                    }
                                    else
                                    {
                                        mDist -= otherNitroLeft;
                                        otherSlowLeft -= otherNitroLeft;
                                        timeToOur += otherNitroLeft * Env.TICKS_PER_REQUEST;

                                        mDist -= otherSlowLeft;
                                        timeToOur += otherSlowLeft * Env.SLOW_TICKS_PER_REQUEST;

                                        timeToOur += mDist * Env.TICKS_PER_REQUEST;
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
                    }

                    if (nextTime > nextTimeLimit || nextTime == nextTimeLimit && (!started || state.territory[nextPos] != player))
                        continue;

                    found = true;
                    coords[len] = nextPos;
                    times[len] = nextTime;
                    used[nextPos] = gen;
                    if (!started && nextStarted)
                        startLen = len;

                    len++;
                    dir = nextDir;
                    pos = nextPos;
                    time = nextTime;
                    timeLimit = nextTimeLimit;
                    nitroLeft = nextNitroLeft;
                    slowLeft = nextSlowLeft;
                    shiftTime = nextShiftTime;
                    started = nextStarted;
                    break;
                }

                if (!found)
                    return false;

                if (started && state.territory[pos] == player)
                    return true;
            }
        }

        public string Print(State state, int player)
        {
            if (len == 0)
                return "<EMPTY>";

            var result = new StringBuilder();
            result.Append(coords[0].ToV());
            for (var i = 1; i < len; i++)
            {
                result.Append("->");
                result.Append(coords[i].ToV());
            }

            return result.ToString();
        }
    }
}