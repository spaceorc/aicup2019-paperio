using System.IO;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
{
    public class DistanceMap
    {
        public int[,] times1;
        public int[,] enterTimes1;
        public int[,] escapeTimes1;
        public int[,] distances1;
        public int[,] enterLineLens1;
        public int[,] lineLens1;
        public int[,] nitroLefts1;
        public int[,] slowLefts1;
        public int[,] paths1;
        public int[,] enterCommands1;

        public int[,] times2;
        public int[,] enterTimes2;
        public int[,] escapeTimes2;
        public int[,] distances2;
        public int[,] enterLineLens2;
        public int[,] lineLens2;
        public int[,] nitroLefts2;
        public int[,] slowLefts2;
        public int[,] paths2;
        public int[,] enterCommands2;

        private Heap<int> priorityQueue = new Heap<int>();
        private ushort[] queue = new ushort[Env.CELLS_COUNT];
        private int[] filled = new int[Env.CELLS_COUNT];
        private int filledGen = 0;

        public ushort[] nearestEmpty;
        public ushort[] nearestOwned;
        public ushort[] nearestOpponentOwned;
        public ushort[,] nearestOpponentActive;

        public void Build(State state)
        {
            if (times1 == null)
            {
                times1 = new int[state.players.Length, Env.CELLS_COUNT];
                enterTimes1 = new int[state.players.Length, Env.CELLS_COUNT];
                escapeTimes1 = new int[state.players.Length, Env.CELLS_COUNT];
                distances1 = new int[state.players.Length, Env.CELLS_COUNT];
                enterLineLens1 = new int[state.players.Length, Env.CELLS_COUNT];
                lineLens1 = new int[state.players.Length, Env.CELLS_COUNT];
                nitroLefts1 = new int[state.players.Length, Env.CELLS_COUNT];
                slowLefts1 = new int[state.players.Length, Env.CELLS_COUNT];
                paths1 = new int[state.players.Length, Env.CELLS_COUNT];
                enterCommands1 = new int[state.players.Length, Env.CELLS_COUNT];
                nearestEmpty = new ushort[state.players.Length];
                nearestOwned = new ushort[state.players.Length];
                nearestOpponentOwned = new ushort[state.players.Length];
                nearestOpponentActive = new ushort[state.players.Length, state.players.Length];
                times2 = new int[state.players.Length, Env.CELLS_COUNT];
                enterTimes2 = new int[state.players.Length, Env.CELLS_COUNT];
                escapeTimes2 = new int[state.players.Length, Env.CELLS_COUNT];
                distances2 = new int[state.players.Length, Env.CELLS_COUNT];
                enterLineLens2 = new int[state.players.Length, Env.CELLS_COUNT];
                lineLens2 = new int[state.players.Length, Env.CELLS_COUNT];
                nitroLefts2 = new int[state.players.Length, Env.CELLS_COUNT];
                slowLefts2 = new int[state.players.Length, Env.CELLS_COUNT];
                paths2 = new int[state.players.Length, Env.CELLS_COUNT];
                enterCommands2 = new int[state.players.Length, Env.CELLS_COUNT];
            }

            for (var i = 0; i < state.players.Length; i++)
            {
                if (state.players[i].status == PlayerStatus.Eliminated)
                    continue;
                Build(state, i);
                Build2(state, i);
            }
        }

        public string Print(State state, int forPlayer)
        {
            var players = state.players;
            var bonuses = state.bonuses;
            var bonusCount = state.bonusCount;
            var lines = state.lines;
            var territory = state.territory;
            const string tc = "ABCDEF";
            using (var writer = new StringWriter())
            {
                for (var y = Env.Y_CELLS_COUNT - 1; y >= 0; y--)
                {
                    for (var x = 0; x < Env.X_CELLS_COUNT; x++)
                    {
                        var c = (ushort)(y * Env.X_CELLS_COUNT + x);

                        var dist = times1[forPlayer, c];

                        var player = -1;
                        for (var p = 0; p < players.Length; p++)
                        {
                            if (players[p].status != PlayerStatus.Eliminated && (players[p].pos == c || players[p].arrivePos == c))
                            {
                                player = p;
                                break;
                            }
                        }

                        Bonus bonus = null;
                        for (var b = 0; b < bonusCount; b++)
                        {
                            if (bonuses[b].pos == c)
                            {
                                bonus = bonuses[b];
                                break;
                            }
                        }

                        if (bonus?.type == BonusType.N)
                            writer.Write('N');
                        else if (bonus?.type == BonusType.S)
                            writer.Write('S');
                        else if (bonus?.type == BonusType.Saw)
                            writer.Write('W');
                        else if (player != -1)
                            writer.Write(player);
                        else if (lines[c] != 0)
                            writer.Write('x');
                        else if (territory[c] == 0xFF)
                            writer.Write('.');
                        else
                            writer.Write(tc[territory[c]]);

                        if (dist == int.MaxValue)
                            writer.Write("_____");
                        else
                            writer.Write($"_{dist.ToString("").PadRight(3, '_')}_");

                        var commands = enterCommands1[forPlayer, c];
                        for (int d = 0; d < 4; d++)
                        {
                            if (((1 << d) & commands) == 0)
                                writer.Write("_");
                            else
                                writer.Write(((Direction)d).ToString()[0]);
                        }

                        writer.Write(" ");
                    }

                    writer.WriteLine();
                    
                    for (var x = 0; x < Env.X_CELLS_COUNT; x++)
                    {
                        var c = (ushort)(y * Env.X_CELLS_COUNT + x);

                        var dist = times2[forPlayer, c];

                        writer.Write("_");

                        if (dist == int.MaxValue)
                            writer.Write("_____");
                        else
                            writer.Write($"_{dist.ToString("").PadRight(3, '_')}_");

                        var commands = enterCommands2[forPlayer, c];
                        for (int d = 0; d < 4; d++)
                        {
                            if (((1 << d) & commands) == 0)
                                writer.Write("_");
                            else
                                writer.Write(((Direction)d).ToString()[0]);
                        }

                        writer.Write(" ");
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }

                return writer.ToString();
            }
        }

        private void Build(State state, int player)
        {
            nearestEmpty[player] = ushort.MaxValue;
            nearestOpponentOwned[player] = ushort.MaxValue;
            for (int opp = 0; opp < state.players.Length; opp++)
                nearestOpponentActive[player, opp] = ushort.MaxValue;
            nearestOwned[player] = ushort.MaxValue;

            for (var c = 0; c < Env.CELLS_COUNT; c++)
            {
                var isLine = (state.lines[c] & (1 << player)) != 0;
                times1[player, c] = isLine ? -1 : int.MaxValue;
                enterTimes1[player, c] = isLine ? -1 : int.MaxValue;
                escapeTimes1[player, c] = isLine ? -1 : int.MaxValue;
                distances1[player, c] = isLine ? -1 : int.MaxValue;
                paths1[player, c] = -1;
                enterCommands1[player, c] = 0;
                lineLens1[player, c] = 0;
                enterLineLens1[player, c] = 0;
            }

            var start = state.players[player].arrivePos;
            if (state.players[player].status == PlayerStatus.Broken)
            {
                times1[player, start] = 0;
                enterTimes1[player, start] = 0;
                distances1[player, start] = 0;
                return;
            }

            if (start == ushort.MaxValue || state.players[player].arriveTime != 0 && times1[player, start] == -1)
                return;

            nitroLefts1[player, start] = state.players[player].nitroLeft;
            slowLefts1[player, start] = state.players[player].slowLeft;
            if (state.players[player].arriveTime != 0)
            {
                if (nitroLefts1[player, start] > 0)
                    nitroLefts1[player, start]--;
                if (slowLefts1[player, start] > 0)
                    slowLefts1[player, start]--;
                enterLineLens1[player, start] = state.players[player].lineCount;
                if (state.territory[start] == player)
                    lineLens1[player, start] = 0;
                else
                    lineLens1[player, start] = state.players[player].lineCount + 1;
            }
            else
                lineLens1[player, start] = state.players[player].lineCount;

            times1[player, start] = state.players[player].arriveTime;
            distances1[player, start] = 0;
            priorityQueue.Clear();
            priorityQueue.Add((state.players[player].arriveTime << 16) | start);
            var startDir = state.players[player].dir;

            while (!priorityQueue.IsEmpty)
            {
                var curItem = priorityQueue.DeleteMin();
                var cur = (ushort)(curItem & 0xFFFF);

                if (nearestEmpty[player] == ushort.MaxValue && state.territory[cur] != player)
                    nearestEmpty[player] = cur;

                if (nearestOpponentOwned[player] == ushort.MaxValue && state.territory[cur] != 0xFF && state.territory[cur] != player)
                    nearestOpponentOwned[player] = cur;

                if (nearestOwned[player] == ushort.MaxValue && state.territory[cur] == player)
                    nearestOwned[player] = cur;

                for (int opp = 0; opp < state.players.Length; opp++)
                {
                    if (opp == player || state.players[opp].status == PlayerStatus.Eliminated)
                        continue;

                    if (nearestOpponentActive[player, opp] == ushort.MaxValue)
                    {
                        if (state.players[opp].arrivePos == cur || (state.lines[cur] & (1 << opp)) != 0)
                            nearestOpponentActive[player, opp] = cur;
                    }
                }

                for (var dir = 0; dir < 4; dir++)
                {
                    if (cur == start && startDir != null)
                    {
                        if (dir == ((int)startDir.Value + 2) % 4)
                            continue;
                    }

                    var next = cur.NextCoord((Direction)dir);
                    if (next != ushort.MaxValue)
                    {
                        var nitroLeft = nitroLefts1[player, cur];
                        var slowLeft = slowLefts1[player, cur];
                        var nextTime = times1[player, cur] + Player.GetShiftTime(nitroLeft, slowLeft);
                        if (nextTime < times1[player, next])
                        {
                            enterTimes1[player, next] = times1[player, next];
                            times1[player, next] = nextTime;
                            distances1[player, next] = distances1[player, cur] + 1;
                            paths1[player, next] = cur;
                            enterCommands1[player, next] = 1 << dir;
                            enterLineLens1[player, next] = lineLens1[player, cur];
                            if (state.territory[next] == player)
                                lineLens1[player, next] = 0;
                            else
                                lineLens1[player, next] = enterLineLens1[player, next] + 1;

                            priorityQueue.Add((nextTime << 16) | next);

                            if (nitroLeft > 0)
                                nitroLeft--;
                            if (slowLeft > 0)
                                slowLeft--;
                            for (var b = 0; b < state.bonusCount; b++)
                            {
                                if (state.bonuses[b].pos == next)
                                {
                                    if (state.bonuses[b].type == BonusType.S)
                                    {
                                        slowLeft += state.bonuses[b].ActiveTicks(player);
                                    }
                                    else if (state.bonuses[b].type == BonusType.N)
                                    {
                                        nitroLeft += state.bonuses[b].ActiveTicks(player);
                                    }
                                }
                            }

                            nitroLefts1[player, next] = nitroLeft;
                            slowLefts1[player, next] = slowLeft;

                            escapeTimes1[player, next] = nextTime + Player.GetShiftTime(nitroLeft, slowLeft);
                        }
                        else if (nextTime == times1[player, next])
                        {
                            enterCommands1[player, next] |= 1 << dir;
                            if (lineLens1[player, cur] < enterLineLens1[player, next])
                            {
                                enterLineLens1[player, next] = lineLens1[player, cur];
                                if (state.territory[next] == player)
                                    lineLens1[player, next] = 0;
                                else
                                    lineLens1[player, next] = enterLineLens1[player, next] + 1;
                                priorityQueue.Add((nextTime << 16) | next);
                            }
                        }
                    }
                }
            }
        }

        private void Build2(State state, int player)
        {
            for (var c = 0; c < Env.CELLS_COUNT; c++)
            {
                times2[player, c] = int.MaxValue;
                enterTimes2[player, c] = int.MaxValue;
                escapeTimes2[player, c] = int.MaxValue;
                distances2[player, c] = int.MaxValue;
                paths2[player, c] = -1;
                enterCommands2[player, c] = 0;
                lineLens2[player, c] = 0;
                enterLineLens2[player, c] = 0;
            }

            if (nearestOwned[player] == ushort.MaxValue || state.players[player].lineCount == 0)
                return;

            var start = nearestOwned[player];

            var head = 0;
            var tail = 0;
            queue[head++] = start;
            filledGen++;
            filled[start] = filledGen;
            while (head != tail)
            {
                var cur = queue[tail++];

                var commands = enterCommands1[player, cur];
                for (int d = 0; d < 4; d++)
                {
                    if ((commands & (1 << (d + 2) % 4)) == 0)
                        continue;

                    var next = cur.NextCoord((Direction)d);
                    if (filled[next] != filledGen)
                    {
                        filled[next] = filledGen;
                        queue[head++] = next;
                    }
                }
            }

            tail = 0;
            head = 0;

            for (ushort x = 0; x < Env.X_CELLS_COUNT; x++)
            {
                var c = x;
                if (state.territory[c] != player && (state.lines[c] & (1 << player)) == 0 && filled[c] != filledGen)
                {
                    queue[head++] = c;
                    filled[c] = filledGen + 1;
                }

                c = (ushort)(x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT);
                if (state.territory[c] != player && (state.lines[c] & (1 << player)) == 0 && filled[c] != filledGen)
                {
                    queue[head++] = c;
                    filled[c] = filledGen + 1;
                }
            }

            for (var y = 1; y < Env.Y_CELLS_COUNT - 1; y++)
            {
                var c = (ushort)(y * Env.X_CELLS_COUNT);
                if (state.territory[c] != player && (state.lines[c] & (1 << player)) == 0 && filled[c] != filledGen)
                {
                    queue[head++] = c;
                    filled[c] = filledGen + 1;
                }

                c = (ushort)(Env.X_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT);
                if (state.territory[c] != player && (state.lines[c] & (1 << player)) == 0 && filled[c] != filledGen)
                {
                    queue[head++] = c;
                    filled[c] = filledGen + 1;
                }
            }

            filledGen++;
            while (tail < head)
            {
                var cur = queue[tail++];
                for (var s = 0; s < 4; s++)
                {
                    var next = cur.NextCoord((Direction)s);
                    if (next != ushort.MaxValue)
                    {
                        if (filled[next] == filledGen)
                            continue;
                        if (state.territory[next] != player && (state.lines[next] & (1 << player)) == 0 && filled[next] != filledGen - 1)
                        {
                            filled[next] = filledGen;
                            queue[head++] = next;
                        }
                    }
                }
            }

            nitroLefts2[player, start] = nitroLefts1[player, start];
            slowLefts2[player, start] = slowLefts1[player, start];

            times2[player, start] = times1[player, start];
            enterTimes2[player, start] = enterTimes1[player, start];
            escapeTimes2[player, start] = escapeTimes1[player, start];
            distances2[player, start] = distances1[player, start];
            enterLineLens2[player, start] = enterLineLens1[player, start];
            enterCommands2[player, start] = enterCommands1[player, start];

            priorityQueue.Clear();
            priorityQueue.Add((times2[player, start] << 16) | start);
            var startEnterCommands = enterCommands2[player, start];

            while (!priorityQueue.IsEmpty)
            {
                var curItem = priorityQueue.DeleteMin();
                var cur = (ushort)(curItem & 0xFFFF);

                if (state.territory[cur] != player)
                {
                    if (nearestEmpty[player] == ushort.MaxValue || times2[player, cur] < times1[player, cur])
                        nearestEmpty[player] = cur;
                }

                if (state.territory[cur] != 0xFF && state.territory[cur] != player)
                {
                    if (nearestOpponentOwned[player] == ushort.MaxValue || times2[player, cur] < times1[player, cur])
                        nearestOpponentOwned[player] = cur;
                }

                for (int opp = 0; opp < state.players.Length; opp++)
                {
                    if (opp == player || state.players[opp].status == PlayerStatus.Eliminated)
                        continue;

                    if (state.players[opp].arrivePos == cur || (state.lines[cur] & (1 << opp)) != 0)
                    {
                        if (nearestOpponentActive[player, opp] == ushort.MaxValue || times2[player, cur] < times1[player, cur])
                            nearestOpponentActive[player, opp] = cur;
                    }
                }

                for (var dir = 0; dir < 4; dir++)
                {
                    if (cur == start && (startEnterCommands & (startEnterCommands - 1)) == 0)
                    {
                        if (1 << (dir + 2) % 4 == startEnterCommands)
                            continue;
                    }

                    var next = cur.NextCoord((Direction)dir);
                    if (next != ushort.MaxValue)
                    {
                        var nitroLeft = nitroLefts2[player, cur];
                        var slowLeft = slowLefts2[player, cur];
                        var nextTime = times2[player, cur] + Player.GetShiftTime(nitroLeft, slowLeft);
                        if (nextTime < times2[player, next])
                        {
                            enterTimes2[player, next] = times2[player, next];
                            times2[player, next] = nextTime;
                            distances2[player, next] = distances2[player, cur] + 1;
                            paths2[player, next] = cur;
                            enterCommands2[player, next] = 1 << dir;
                            enterLineLens2[player, next] = lineLens2[player, cur];
                            if (state.territory[next] == player || filled[next] != filledGen)
                                lineLens2[player, next] = 0;
                            else
                                lineLens2[player, next] = enterLineLens2[player, next] + 1;

                            priorityQueue.Add((nextTime << 16) | next);

                            if (nitroLeft > 0)
                                nitroLeft--;
                            if (slowLeft > 0)
                                slowLeft--;
                            for (var b = 0; b < state.bonusCount; b++)
                            {
                                if (state.bonuses[b].pos == next)
                                {
                                    if (state.bonuses[b].type == BonusType.S)
                                    {
                                        slowLeft += state.bonuses[b].ActiveTicks(player);
                                    }
                                    else if (state.bonuses[b].type == BonusType.N)
                                    {
                                        nitroLeft += state.bonuses[b].ActiveTicks(player);
                                    }
                                }
                            }

                            nitroLefts2[player, next] = nitroLeft;
                            slowLefts2[player, next] = slowLeft;

                            escapeTimes2[player, next] = nextTime + Player.GetShiftTime(nitroLeft, slowLeft);
                        }
                        else if (nextTime == times2[player, next])
                        {
                            enterCommands2[player, next] |= 1 << dir;
                            if (lineLens2[player, cur] < enterLineLens2[player, next])
                            {
                                enterLineLens2[player, next] = lineLens2[player, cur];
                                if (state.territory[next] == player || filled[next] != filledGen)
                                    lineLens2[player, next] = 0;
                                else
                                    lineLens2[player, next] = enterLineLens2[player, next] + 1;
                                priorityQueue.Add((nextTime << 16) | next);
                            }
                        }
                    }
                }
            }
        }
    }
}