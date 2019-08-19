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
        public Direction? fixedNextDir;
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
            fixedNextDir = null;
            time = 0;
            shiftTime = state.players[player].shiftTime;
            nitroLeft = state.players[player].nitroLeft;
            slowLeft = state.players[player].slowLeft;
        }

        public bool TryAdd(State state, int player, DistanceMap distanceMap, InterestingFacts facts, ushort nextPos)
        {
            if (used[nextPos] == gen)
                return false;

            var nextDir = pos.DirTo(nextPos);
            if (fixedNextDir != null && fixedNextDir.Value != nextDir)
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
            Direction? nextFixedNextDir = null;
            if (nextStarted)
            {
                for (var other = 0; other < state.players.Length; other++)
                {
                    if (other == player || state.players[other].status == PlayerStatus.Eliminated)
                        continue;

                    var otherTimeToPos = distanceMap.times1[other, nextPos];
                    if (otherTimeToPos != -1 && otherTimeToPos != int.MaxValue)
                    {
                        if (otherTimeToPos < nextTimeLimit)
                            nextTimeLimit = otherTimeToPos;

                        if (state.players[other].arrivePos == nextPos)
                        {
                            nextTimeLimit = -1;
                            break;
                        }

                        var finished = state.territory[nextPos] == player && (!useTerritoryTtl || facts.territoryTtl[nextPos] > nextTime);
                        var enterLineLen = len - startLen + state.players[player].lineCount;
                        var lineLen = finished ? 0 : enterLineLen + 1;
                        var canKillOnEnter = distanceMap.enterLineLens1[other, nextPos] <= enterLineLen;
                        var canKillOnEscape = distanceMap.lineLens1[other, nextPos] <= lineLen;
                        var canKillInside = distanceMap.enterLineLens1[other, nextPos] <= lineLen;

                        if (otherTimeToPos < escapeTime && canKillOnEscape)
                        {
                            nextTimeLimit = -1;
                            break;
                        }

                        var otherEnterTime = distanceMap.enterTimes1[other, nextPos];
                        if (otherEnterTime != -1 && otherEnterTime != int.MaxValue)
                        {
                            if (otherEnterTime < nextTime && canKillOnEnter)
                            {
                                nextTimeLimit = -1;
                                break;
                            }

                            if (otherEnterTime == nextTime && canKillInside)
                            {
                                nextTimeLimit = -1;
                                break;
                            }

                            if (otherEnterTime < escapeTime && canKillInside)
                            {
                                var enterCommands = distanceMap.enterCommands1[other, nextPos];
                                if ((enterCommands & (enterCommands - 1)) != 0)
                                {
                                    nextTimeLimit = -1;
                                    break;
                                }

                                for (int d = 0; d < 4; d++)
                                {
                                    if (1 << d == enterCommands)
                                    {
                                        nextFixedNextDir = (Direction)d;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    otherTimeToPos = distanceMap.times2[other, nextPos];
                    if (otherTimeToPos != -1 && otherTimeToPos != int.MaxValue)
                    {
                        if (otherTimeToPos < nextTimeLimit)
                            nextTimeLimit = otherTimeToPos;

                        if (state.players[other].arrivePos == nextPos)
                        {
                            nextTimeLimit = -1;
                            break;
                        }

                        var finished = state.territory[nextPos] == player && (!useTerritoryTtl || facts.territoryTtl[nextPos] > nextTime);
                        var enterLineLen = len - startLen + state.players[player].lineCount;
                        var lineLen = finished ? 0 : enterLineLen + 1;
                        var canKillOnEnter = distanceMap.enterLineLens2[other, nextPos] <= enterLineLen;
                        var canKillOnEscape = distanceMap.lineLens2[other, nextPos] <= lineLen;
                        var canKillInside = distanceMap.enterLineLens2[other, nextPos] <= lineLen;

                        if (otherTimeToPos < escapeTime && canKillOnEscape)
                        {
                            nextTimeLimit = -1;
                            break;
                        }

                        var otherEnterTime = distanceMap.enterTimes2[other, nextPos];
                        if (otherEnterTime != -1 && otherEnterTime != int.MaxValue)
                        {
                            if (otherEnterTime < nextTime && canKillOnEnter)
                            {
                                nextTimeLimit = -1;
                                break;
                            }

                            if (otherEnterTime == nextTime && canKillInside)
                            {
                                nextTimeLimit = -1;
                                break;
                            }

                            if (otherEnterTime < escapeTime && canKillInside)
                            {
                                var enterCommands = distanceMap.enterCommands2[other, nextPos];
                                if ((enterCommands & (enterCommands - 1)) != 0)
                                {
                                    nextTimeLimit = -1;
                                    break;
                                }

                                for (int d = 0; d < 4; d++)
                                {
                                    if (1 << d == enterCommands)
                                    {
                                        nextFixedNextDir = (Direction)d;
                                        break;
                                    }
                                }
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
            fixedNextDir = nextFixedNextDir;
            dir = nextDir;
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

                    if (distanceMap.times1[other, line] != -1 && distanceMap.times1[other, line] < timeLimit)
                        timeLimit = distanceMap.times1[other, line];

                    if (distanceMap.times2[other, line] != -1 && distanceMap.times2[other, line] < timeLimit)
                        timeLimit = distanceMap.times2[other, line];
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