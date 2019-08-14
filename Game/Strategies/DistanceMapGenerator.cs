using System;
using System.IO;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies
{
    public class DistanceMapGenerator
    {
        public int[,] times;
        public int[,] nitroLefts;
        public int[,] slowLefts;
        public int[,] paths;
        public ushort[] queue;
        public ushort[] nearestEmpty;
        public ushort[] nearestOwned;
        public ushort[] nearestOpponent;

        public void Build(State state)
        {
            if (times == null)
            {
                times = new int[state.players.Length, Env.CELLS_COUNT];
                nitroLefts = new int[state.players.Length, Env.CELLS_COUNT];
                slowLefts = new int[state.players.Length, Env.CELLS_COUNT];
                paths = new int[state.players.Length, Env.CELLS_COUNT];
                queue = new ushort[Env.CELLS_COUNT];
                nearestEmpty = new ushort[state.players.Length];
                nearestOwned = new ushort[state.players.Length];
                nearestOpponent = new ushort[state.players.Length];
            }

            for (var i = 0; i < state.players.Length; i++)
            {
                if (state.players[i].status == PlayerStatus.Eliminated)
                    continue;
                Build(state, i);
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

                        var dist = times[forPlayer, c];

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
                            writer.Write("     ");
                        else
                            writer.Write($"_{dist.ToString("").PadRight(3)} ");
                    }

                    writer.WriteLine();
                }

                return writer.ToString();
            }
        }

        private void Build(State state, int player)
        {
            nearestEmpty[player] = ushort.MaxValue;
            nearestOpponent[player] = ushort.MaxValue;
            nearestOwned[player] = ushort.MaxValue;
            for (var c = 0; c < times.GetLength(1); c++)
            {
                var isLine = (state.lines[c] & (1 << player)) != 0;
                times[player, c] = isLine ? -1 : int.MaxValue;
                paths[player, c] = isLine ? -1 : int.MaxValue;
            }

            var head = 0;
            var tail = 0;

            var start = state.players[player].arrivePos;

            if (state.players[player].status == PlayerStatus.Broken)
            {
                times[player, start] = 0;
                return;
            }

            if (state.players[player].arriveTime != 0 && times[player, start] == -1)
                return;

            nitroLefts[player, start] = state.players[player].nitroLeft;
            slowLefts[player, start] = state.players[player].slowLeft;
            if (state.players[player].arriveTime != 0)
            {
                if (nitroLefts[player, start] > 0)
                    nitroLefts[player, start]--;
                if (slowLefts[player, start] > 0)
                    slowLefts[player, start]--;
            }

            queue[head++] = start;
            times[player, start] = state.players[player].arriveTime;
            var startDir = state.players[player].dir;
            while (head != tail)
            {
                var cur = queue[tail];
                tail = (tail + 1) % queue.Length;

                if (nearestEmpty[player] == ushort.MaxValue && state.territory[cur] != player)
                    nearestEmpty[player] = cur;
                if (nearestOpponent[player] == ushort.MaxValue && state.territory[cur] != 0xFF && state.territory[cur] != player)
                    nearestOpponent[player] = cur;
                if (nearestOwned[player] == ushort.MaxValue && state.territory[cur] == player)
                    nearestOwned[player] = cur;

                for (var dir = 0; dir < 4; dir++)
                {
                    if (cur == start && startDir != null)
                    {
                        if (dir == ((int)startDir.Value + 2) % 4)
                            continue;
                    }

                    var next = state.NextCoord(cur, (Direction)dir);
                    if (next != ushort.MaxValue)
                    {
                        var nitroLeft = nitroLefts[player, cur];
                        var slowLeft = slowLefts[player, cur];
                        var nextDist = times[player, cur] + Player.GetShiftTime(nitroLeft, slowLeft);
                        if (nextDist < times[player, next])
                        {
                            times[player, next] = nextDist;
                            paths[player, next] = cur;

                            queue[head] = next;
                            head = (head + 1) % queue.Length;
                            if (head == tail)
                                throw new InvalidOperationException("Queue overflow");

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
                                        slowLeft += player == 0 ? 50 : 10;
                                    }
                                    else if (state.bonuses[b].type == BonusType.N)
                                    {
                                        nitroLeft += player == 0 ? 10 : 50;
                                    }
                                }
                            }

                            nitroLefts[player, next] = nitroLeft;
                            slowLefts[player, next] = slowLeft;
                        }
                    }
                }
            }
        }
    }
}