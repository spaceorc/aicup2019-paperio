using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Sim.Undo;

namespace Game.Sim
{
    public class TerritoryCapture
    {
        private int gen;
        private int[] territoryCaptureMask;
        private int[] territoryCaptureCount;
        private ushort[,] territoryCapture;

        private int queueGen;
        private ushort[] queue;
        private int[] used;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CapturedCountBy(int player) => territoryCaptureCount[player];

        public void Init(int playerCount)
        {
            gen = 0;
            queueGen = 0;
            if (territoryCaptureMask == null
                || territoryCaptureMask.Length != Env.CELLS_COUNT)
            {
                territoryCaptureMask = new int[Env.CELLS_COUNT];
                queue = new ushort[Env.CELLS_COUNT];
                used = new int[Env.CELLS_COUNT];
            }
            else
            {
                for (var x = 0; x < Env.X_CELLS_COUNT; x++)
                for (var y = 0; y < Env.Y_CELLS_COUNT; y++)
                {
                    territoryCaptureMask[x + y * Env.X_CELLS_COUNT] = 0;
                    used[x + y * Env.X_CELLS_COUNT] = 0;
                }
            }

            if (territoryCaptureCount == null || territoryCaptureCount.Length != playerCount)
            {
                territoryCaptureCount = new int[playerCount];
            }
            else
            {
                for (var i = 0; i < territoryCaptureCount.Length; i++)
                    territoryCaptureCount[i] = 0;
            }

            if (territoryCapture == null
                || territoryCapture.GetLength(0) != playerCount
                || territoryCapture.GetLength(1) != Env.CELLS_COUNT)
            {
                territoryCapture = new ushort[playerCount, Env.CELLS_COUNT];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            gen += 1 << 8;
            for (var i = 0; i < territoryCaptureCount.Length; i++)
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
        public int PlayerEatenByMask(ushort v, int player)
        {
            var mask = territoryCaptureMask[v];
            if ((mask & ~0xFF) != gen)
                return 0;

            mask &= 0xFF;
            return mask & ~(1 << player);
        }

        public void ApplyTo(State state, StateUndo undo)
        {
            for (var player = 0; player < territoryCaptureCount.Length; player++)
            {
                if (state.players[player].status == PlayerStatus.Eliminated)
                    continue;

                for (var i = 0; i < territoryCaptureCount[player]; i++)
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
                            undo?.NotifyCapture(state);

                            state.territory[v] = (byte)player;
                            state.territoryVersion++;
                        }
                    }
                }
            }
        }

        public void Capture(State state, int player)
        {
            if (state.players[player].lineCount == 0)
                return;
            if (state.territory[state.players[player].arrivePos] != player)
                return;

            queueGen++;

            var tail = 0;
            var head = 0;

            for (ushort x = 0; x < Env.X_CELLS_COUNT; x++)
            {
                if (state.territory[x] != player && (state.lines[x] & (1 << player)) == 0)
                {
                    queue[head++] = x;
                    used[x] = queueGen;
                }

                if (state.territory[x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT] != player && (state.lines[x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT);
                    used[x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT] = queueGen;
                }
            }

            for (var y = 1; y < Env.Y_CELLS_COUNT - 1; y++)
            {
                if (state.territory[y * Env.X_CELLS_COUNT] != player && (state.lines[y * Env.X_CELLS_COUNT] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(y * Env.X_CELLS_COUNT);
                    used[y * Env.X_CELLS_COUNT] = queueGen;
                }

                if (state.territory[Env.X_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT] != player && (state.lines[Env.X_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT] & (1 << player)) == 0)
                {
                    queue[head++] = (ushort)(Env.X_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT);
                    used[Env.X_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT] = queueGen;
                }
            }

            while (tail < head)
            {
                var cur = queue[tail++];
                for (var s = 0; s < 4; s++)
                {
                    var next = cur.NextCoord((Direction)s);
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

            for (ushort c = 0; c < Env.CELLS_COUNT; c++)
            {
                if (used[c] != queueGen && state.territory[c] != player)
                    Add(c, player);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(ushort v, int player)
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
    }
}