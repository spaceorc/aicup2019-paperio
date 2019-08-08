using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastTerritoryCapture
    {
        private int gen;

        public int[] territoryCaptureMask;
        public int[] territoryCaptureCount;
        public ushort[,] territoryCapture;

        private int queueGen;
        private ushort[] queue;
        private int[] used;

        public void Init(Config config, int playerCount)
        {
            gen = 0;
            queueGen = 0;
            if (territoryCaptureMask == null
                || territoryCaptureMask.Length != config.x_cells_count * config.y_cells_count)
            {
                territoryCaptureMask = new int[config.x_cells_count * config.y_cells_count];
                queue = new ushort[config.x_cells_count * config.y_cells_count];
                used = new int[config.x_cells_count * config.y_cells_count];
            }
            else
            {
                for (int x = 0; x < config.x_cells_count; x++)
                for (int y = 0; y < config.y_cells_count; y++)
                {
                    territoryCaptureMask[x + y * config.x_cells_count] = 0;
                    used[x + y * config.x_cells_count] = 0;
                }
            }

            if (territoryCaptureCount == null || territoryCaptureCount.Length != playerCount)
            {
                territoryCaptureCount = new int[playerCount];
            }
            else
            {
                for (int i = 0; i < territoryCaptureCount.Length; i++)
                    territoryCaptureCount[i] = 0;
            }

            if (territoryCapture == null
                || territoryCapture.GetLength(0) != playerCount
                || territoryCapture.GetLength(1) != config.x_cells_count * config.y_cells_count)
            {
                territoryCapture = new ushort[playerCount, config.x_cells_count * config.y_cells_count];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            gen += 1 << 8;
            for (int i = 0; i < territoryCaptureCount.Length; i++)
                territoryCaptureCount[i] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BelongsTo(ushort v, int player)
        {
            var mask = territoryCaptureMask[v];
            return ((mask & ~0xFF) == gen)
                   & ((mask & 0xFF) - (1 << player) == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EatenBy(ushort v)
        {
            var mask = territoryCaptureMask[v];
            if ((mask & ~0xFF) != gen)
                return -1;

            mask &= 0xFF;
            if (mask == 0)
                return -1;

            if ((mask & (mask - 1)) != 0)
                return -1;

            for (int i = 0; i < territoryCaptureCount.Length; i++)
            {
                if (mask == 1 << i)
                    return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ushort v, int player)
        {
            var mask = territoryCaptureMask[v];
            if ((mask & ~0xFF) == gen)
            {
                if ((mask & (1 << player)) == 0)
                {
                    territoryCaptureMask[v] = mask | (1 << player);
                    territoryCapture[player, territoryCaptureCount[player]++] = v;
                }
            }
            else
            {
                territoryCaptureMask[v] = (1 << player) | gen;
                territoryCapture[player, territoryCaptureCount[player]++] = v;
            }
        }

        public void ApplyTo(FastState state)
        {
            for (int player = 0; player < territoryCaptureCount.Length; player++)
            {
                if (state.players[player].status == PlayerStatus.Eliminated)
                    continue;

                for (int i = 0; i < territoryCaptureCount[player]; i++)
                {
                    var v = territoryCapture[player, i];
                    var mask = territoryCaptureMask[v] & 0xFF;
                    if ((mask & ~(1 << player)) == 0)
                    {
                        var owner = state.territory[v];
                        if (owner != player)
                        {
                            if (owner != 0xFF && state.players[owner].status != PlayerStatus.Eliminated)
                            {
                                state.players[player].tickScore += Env.ENEMY_TERRITORY_SCORE - Env.NEUTRAL_TERRITORY_SCORE;
                                state.players[player].opponentTerritoryCaptured++;
                                state.players[owner].territory--;
                            }

                            state.players[player].territory++;
                            state.territory[v] = (byte)player;
                            state.territoryVersion++;
                        }
                    }
                }
            }
        }

        public void Capture(FastState state, int player)
        {
            if (state.players[player].lineCount == 0)
                return;
            if (state.territory[state.players[player].arrivePos] != player)
                return;

            queueGen++;

            var tail = 0;
            var head = 0;

            var config = state.config;
            for (ushort x = 0; x < config.x_cells_count; x++)
            {
                if (state.territory[x] != player && (state.lines[x] & (1 << player)) == 0)
                {
                    queue[head++] = x;
                    used[x] = queueGen;
                }

                if (state.territory[x + (config.y_cells_count - 1) * config.x_cells_count] != player && (state.lines[x + (config.y_cells_count - 1) * config.x_cells_count] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(x + (config.y_cells_count - 1) * config.x_cells_count);
                    used[x + (config.y_cells_count - 1) * config.x_cells_count] = queueGen;
                }
            }

            for (int y = 0; y < config.y_cells_count; y++)
            {
                if (state.territory[y * config.x_cells_count] != player && (state.lines[y * config.x_cells_count] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(y * config.x_cells_count);
                    used[y * config.x_cells_count] = queueGen;
                }

                if (state.territory[config.x_cells_count - 1 + y * config.x_cells_count] != player && (state.lines[config.x_cells_count - 1 + y * config.x_cells_count] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(config.x_cells_count - 1 + y * config.x_cells_count);
                    used[config.x_cells_count - 1 + y * config.x_cells_count] = queueGen;
                }
            }

            while (tail < head)
            {
                var cur = queue[tail++];
                for (var s = 0; s < 4; s++)
                {
                    var next = state.NextCoord(cur, (Direction)s);
                    if (next != ushort.MaxValue)
                    {
                        if (used[next] == queueGen)
                            continue;
                        if (state.territory[next] != player && (state.lines[next] & (1 << player)) == 0)
                        {
                            used[next] = queueGen;
                            queue[head++] = next;
                        }
                    }
                }
            }

            for (ushort c = 0; c < config.x_cells_count * config.y_cells_count; c++)
            {
                if (used[c] != queueGen && state.territory[c] != player)
                    Add(c, player);
            }
        }
    }
}