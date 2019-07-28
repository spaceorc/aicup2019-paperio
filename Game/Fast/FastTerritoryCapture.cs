using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastTerritoryCapture
    {
        private int gen;

        public int[,] territoryCaptureMask;
        public int[] territoryCaptureCount;
        public V[,] territoryCapture;

        private int queueGen;
        private V[] queue;
        private int[,] used;

        public void Init(Config config, int playerCount)
        {
            gen = 0;
            queueGen = 0;
            if (territoryCaptureMask == null
                || territoryCaptureMask.GetLength(0) != config.x_cells_count
                || territoryCaptureMask.GetLength(1) != config.y_cells_count)
            {
                territoryCaptureMask = new int[config.x_cells_count, config.y_cells_count];
                queue = new V[config.x_cells_count * config.y_cells_count];
                used = new int[config.x_cells_count, config.y_cells_count];
            }
            else
            {
                for (int x = 0; x < config.x_cells_count; x++)
                for (int y = 0; y < config.y_cells_count; y++)
                {
                    territoryCaptureMask[x, y] = 0;
                    used[x, y] = 0;
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
                territoryCapture = new V[playerCount, config.x_cells_count * config.y_cells_count];
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
        public bool BelongsTo(V v, int player)
        {
            var mask = territoryCaptureMask[v.X, v.Y];
            return ((mask & ~0xFF) == gen)
                   & ((mask & 0xFF) - (1 << player) == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEaten(V v, int player)
        {
            var mask = territoryCaptureMask[v.X, v.Y];
            if ((mask & ~0xFF) != gen)
                return false;

            mask &= 0xFF;
            return (mask & ~(1 << player)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(V v, int player)
        {
            var mask = territoryCaptureMask[v.X, v.Y];
            if ((mask & ~0xFF) == gen)
            {
                if ((mask & (1 << player)) == 0)
                {
                    territoryCaptureMask[v.X, v.Y] = mask | (1 << player);
                    territoryCapture[player, territoryCaptureCount[player]++] = v;
                }
            }
            else
            {
                territoryCaptureMask[v.X, v.Y] = (1 << player) | gen;
                territoryCapture[player, territoryCaptureCount[player]++] = v;
            }
        }

        public void ApplyTo(FastState state)
        {
            for (int player = 0; player < territoryCaptureCount.Length; player++)
            {
                for (int i = 0; i < territoryCaptureCount[i]; i++)
                {
                    var v = territoryCapture[player, i];
                    var mask = territoryCaptureMask[v.X, v.Y] & 0xFF;
                    if ((mask & ~(1 << player)) == 0)
                    {
                        if (state.territory[v.X, v.Y] != player)
                        {
                            if (state.territory[v.X, v.Y] != 0xFF)
                            {
                                state.players[player].tickScore += Env.ENEMY_TERRITORY_SCORE - Env.NEUTRAL_TERRITORY_SCORE;
                                state.players[state.territory[v.X, v.Y]].territory--;
                            }

                            state.players[player].territory++;
                            state.territory[v.X, v.Y] = (byte)player;
                            state.territoryVersion++;
                        }
                    }
                }
            }
        }

        public void Capture(FastState state, int player, Config config)
        {
            if (state.players[player].lineCount == 0)
                return;
            if (state.territory[state.players[player].arrivePos.X, state.players[player].arrivePos.Y] != player)
                return;

            queueGen++;

            var tail = 0;
            var head = 0;

            for (int x = 0; x < config.x_cells_count; x++)
            {
                if (state.territory[x, 0] != player && (state.lines[x, 0] & (1 << player)) == 0)
                {
                    queue[head++] = V.Get(x, 0);
                    used[x, 0] = queueGen;
                }

                if (state.territory[x, config.y_cells_count - 1] != player && (state.lines[x, config.y_cells_count - 1] & (1 << player)) == 0)
                {
                    queue[head++] = V.Get(x, config.y_cells_count - 1);
                    used[x, config.y_cells_count - 1] = queueGen;
                }
            }

            for (int y = 0; y < config.y_cells_count; y++)
            {
                if (state.territory[0, y] != player && (state.lines[0, y] & (1 << player)) == 0)
                {
                    queue[head++] = V.Get(0, y);
                    used[0, y] = queueGen;
                }

                if (state.territory[config.x_cells_count - 1, y] != player && (state.lines[config.x_cells_count - 1, y] & (1 << player)) == 0)
                {
                    queue[head++] = V.Get(config.x_cells_count - 1, y);
                    used[config.x_cells_count - 1, y] = queueGen;
                }
            }

            while (tail < head)
            {
                var cur = queue[tail++];
                for (var s = 0; s < V.vertAndHoriz.Length; s++)
                {
                    var shift = V.vertAndHoriz[s];
                    var next = cur + shift;
                    if (next.X >= 0 && next.X < config.x_cells_count && next.Y >= 0 && next.Y < config.y_cells_count)
                    {
                        if (used[next.X, next.Y] == queueGen)
                            continue;
                        if (state.territory[next.X, next.Y] != player && (state.lines[next.X, next.Y] & (1 << player)) == 0)
                        {
                            used[next.X, next.Y] = queueGen;
                            queue[head++] = next;
                        }
                    }
                }
            }

            for (int x = 0; x < config.x_cells_count; x++)
            for (int y = 0; y < config.y_cells_count; y++)
            {
                var v = V.Get(x, y);
                if (used[x, y] != queueGen)
                    Add(v, player);
            }
        }
    }
}