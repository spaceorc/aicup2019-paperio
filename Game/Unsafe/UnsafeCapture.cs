using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;

namespace Game.Unsafe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct UnsafeCapture
    {
        // bit per player + gen 26 bit
        public const uint CAPTURE_OWNER_MASK = 0b00111111;
        public const uint CAPTURE_GEN_INCREMENT = 1 << 6;
        public const uint CAPTURE_GEN_MASK = ~CAPTURE_OWNER_MASK;
        public fixed uint captureMask[Env.CELLS_COUNT];
        public uint captureGen;

        public byte capturedBonusesCount;
        public fixed ushort capturedBonusesAt[3];

        public fixed ushort captureCount[6];
        public const int PLAYER_CAPTURE_CAPACITY = Env.CELLS_COUNT;
        public fixed ushort capture[PLAYER_CAPTURE_CAPACITY * 6];

        public fixed ushort queue[Env.CELLS_COUNT];
        public fixed uint used[Env.CELLS_COUNT];
        public uint usedGen;

        public void Init()
        {
            fixed (UnsafeCapture* that = &this)
            {
                that->captureGen = 0;
                that->usedGen = 0;
                for (var i = 0; i < Env.CELLS_COUNT; i++)
                {
                    that->used[i] = 0;
                    that->captureMask[i] = 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(UnsafeState* state)
        {
            fixed (UnsafeCapture* that = &this)
            {
                that->capturedBonusesCount = 0;
                that->captureGen += CAPTURE_GEN_INCREMENT;
                for (var i = 0; i < state->playersCount; i++)
                    that->captureCount[i] = 0;
                for (var i = 0; i < 3; i++)
                    that->capturedBonusesAt[i] = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BelongsTo(ushort v, int player)
        {
            fixed (UnsafeCapture* that = &this)
            {
                var mask = that->captureMask[v];
                return ((mask & CAPTURE_GEN_MASK) == that->captureGen)
                       & ((mask & CAPTURE_OWNER_MASK) - (1 << player) == 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint EatenBy(ushort v, int player)
        {
            fixed (UnsafeCapture* that = &this)
            {
                var mask = that->captureMask[v];
                if ((mask & CAPTURE_GEN_MASK) != that->captureGen)
                    return 0;

                mask &= CAPTURE_OWNER_MASK;
                return (uint)(mask & ~(1 << player));
            }
        }

        public void ApplyTo(UnsafeState* state, ushort* tickScores)
        {
            fixed (UnsafeCapture* that = &this)
            {
                var ps = (UnsafePlayer*)state->players;
                var p = ps;
                for (int player = 0; player < state->playersCount; player++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    for (int i = 0; i < that->captureCount[player]; i++)
                    {
                        var v = that->capture[PLAYER_CAPTURE_CAPACITY * player + i];
                        var mask = that->captureMask[v] & CAPTURE_OWNER_MASK;
                        if ((mask & ~(1 << player)) == 0)
                        {
                            var owner = state->territory[v] & UnsafeState.TERRITORY_OWNER_MASK;
                            if (owner != player)
                            {
                                if (owner != UnsafeState.TERRITORY_OWNER_NO && ps[owner].status != UnsafePlayer.STATUS_ELIMINATED)
                                {
                                    tickScores[player] += Env.ENEMY_TERRITORY_SCORE - Env.NEUTRAL_TERRITORY_SCORE;
                                    p->opponentTerritoryCaptured++;
                                    ps[owner].territory--;
                                }

                                p->territory++;
                                state->territory[v] = (byte)(state->territory[v] & ~UnsafeState.TERRITORY_OWNER_MASK | player);
                            }
                        }
                    }
                }
            }
        }

        public void Capture(UnsafeState* state, int player, UnsafeUndo* undo)
        {
            fixed (UnsafeCapture* that = &this)
            {
                var players = (UnsafePlayer*)state->players;
                if (players[player].lineCount == 0)
                    return;

                if ((state->territory[players[player].arrivePos] & UnsafeState.TERRITORY_OWNER_MASK) != player)
                    return;

                players[player].lineCount = 0;

                that->usedGen++;

                var tail = 0;
                var head = 0;

                for (ushort x = 0; x < Env.X_CELLS_COUNT; x++)
                {
                    var c = x;
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }

                    c = (ushort)(x + (Env.Y_CELLS_COUNT - 1) * Env.X_CELLS_COUNT);
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }
                }

                for (ushort y = 1; y < Env.Y_CELLS_COUNT - 1; y++)
                {
                    var c = (ushort)(y * Env.X_CELLS_COUNT);
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }

                    c = (ushort)(Env.Y_CELLS_COUNT - 1 + y * Env.X_CELLS_COUNT);
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }
                }

                while (tail < head)
                {
                    var cur = that->queue[tail++];
                    for (byte s = 0; s < 4; s++)
                    {
                        var next = UnsafeState.NextCoord(cur, s);
                        if (next != ushort.MaxValue)
                        {
                            if (that->used[next] == that->usedGen)
                                continue;
                            if ((state->territory[next] & UnsafeState.TERRITORY_OWNER_MASK) != player
                                && (state->territory[next] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                            {
                                that->used[next] = that->usedGen;
                                that->queue[head++] = next;
                            }
                        }
                    }
                }

                if (undo != null)
                    undo->BeforeTerritoryChange(state);
                
                for (ushort c = 0; c < Env.CELLS_COUNT; c++)
                {
                    if (that->used[c] != usedGen && (state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player)
                    {
                        var mask = that->captureMask[c];
                        if ((mask & CAPTURE_GEN_MASK) == that->captureGen)
                        {
                            if ((mask & (1 << player)) == 0)
                            {
                                that->captureMask[c] = (uint)(1 << player) | mask;
                                that->capture[player * PLAYER_CAPTURE_CAPACITY + that->captureCount[player]++] = c;
                            }
                        }
                        else
                        {
                            that->captureMask[c] = (uint)(1 << player) | that->captureGen;
                            that->capture[player * PLAYER_CAPTURE_CAPACITY + that->captureCount[player]++] = c;
                        }

                        if ((state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) == player << UnsafeState.TERRITORY_LINE_SHIFT)
                            state->territory[c] = (byte)(state->territory[c] | UnsafeState.TERRITORY_LINE_NO);

                        if ((state->territory[c] & UnsafeState.TERRITORY_BONUS_MASK) != UnsafeState.TERRITORY_BONUS_NO)
                        {
                            var alreadyCaptured = false;
                            for (int i = 0; i < that->capturedBonusesCount; i++)
                            {
                                if (that->capturedBonusesAt[i] == c)
                                {
                                    alreadyCaptured = true;
                                    break;
                                }
                            }

                            if (!alreadyCaptured)
                                that->capturedBonusesAt[that->capturedBonusesCount++] = c;
                        }
                    }
                }
            }
        }
    }
}