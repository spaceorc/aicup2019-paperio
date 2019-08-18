using System.Text;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
{
    public class ReliablePathBuilder
    {
        public readonly bool useTerritoryTtl;

        public ushort[] coords;
        public int[] times;
        public int[] used;
        public int len;
        public int gen;
        public int startLen;
        public Direction? dir;
        public ushort pos;
        public bool started;
        public int time;
        
        private int timeLimit;
        private int shiftTime;
        private int nitroLeft;
        private int slowLeft;

        public ReliablePathBuilder(bool useTerritoryTtl)
        {
            this.useTerritoryTtl = useTerritoryTtl;
            coords = new ushort[Env.CELLS_COUNT];
            times = new int[Env.CELLS_COUNT];
            used = new int[Env.CELLS_COUNT];
        }

        public void Start(State state, int player, DistanceMap distanceMap)
        {
            PrepareTimeLimit(state, player, distanceMap);
            len = 0;
            startLen = 0;
            pos = state.players[player].arrivePos;
            started = state.players[player].lineCount > 0;
            dir = state.players[player].dir;
            time = 0;
            shiftTime = state.players[player].shiftTime;
            nitroLeft = state.players[player].nitroLeft;
            slowLeft = state.players[player].slowLeft;
        }

        public bool TryAdd(State state, int player, DistanceMap distanceMap, InterestingFacts facts, ushort nextPos)
        {
            if (used[nextPos] == gen)
                return false;

            var nextTime = time + shiftTime;
            if (nextTime > timeLimit || nextTime == timeLimit && (!started || state.territory[nextPos] != player || useTerritoryTtl && facts.territoryTtl[nextPos] <= nextTime))
                return false;

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
                        nextSlowLeft += state.bonuses[b].ActiveTicks(player);
                    else if (state.bonuses[b].type == BonusType.N)
                        nextNitroLeft += state.bonuses[b].ActiveTicks(player);
                }
            }

            var nextShiftTime = Player.GetShiftTime(nextNitroLeft, nextSlowLeft);
            var escapeTime = nextTime + nextShiftTime;

            var nextTimeLimit = timeLimit;
            var nextStarted = started || state.territory[nextPos] != player
                                      || useTerritoryTtl && facts.territoryTtl[nextPos] <= nextTime;
            if (nextStarted)
            {
                for (var other = 0; other < state.players.Length; other++)
                {
                    if (other == player || state.players[other].status == PlayerStatus.Eliminated)
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
                        if (prevOtherPos == -1 || prevOtherPos == int.MaxValue)
                        {
                            nextTimeLimit = -1;
                            break;
                        }

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

            if (nextTime > nextTimeLimit
                || nextTime == nextTimeLimit && (!started || state.territory[nextPos] != player || useTerritoryTtl && facts.territoryTtl[nextPos] <= nextTime))
                return false;

            coords[len] = nextPos;
            times[len] = nextTime;
            used[nextPos] = gen;
            if (!started && nextStarted)
                startLen = len;

            len++;
            dir = pos.DirTo(nextPos);
            pos = nextPos;
            time = nextTime;
            timeLimit = nextTimeLimit;
            nitroLeft = nextNitroLeft;
            slowLeft = nextSlowLeft;
            shiftTime = nextShiftTime;
            started = nextStarted;
            return true;
        }

        private void PrepareTimeLimit(State state, int player, DistanceMap distanceMap)
        {
            gen++;
            timeLimit = Env.MAX_TICK_COUNT - state.time;
            for (var i = 0; i < state.players[player].lineCount; i++)
            {
                var line = state.players[player].line[i];
                used[line] = gen;
                for (var other = 0; other < state.players.Length; other++)
                {
                    if (other == player || state.players[other].status == PlayerStatus.Eliminated)
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