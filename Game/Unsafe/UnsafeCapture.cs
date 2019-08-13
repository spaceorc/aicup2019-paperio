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
        public fixed uint captureMask[31 * 31];
        public uint captureGen;

        public byte capturedBonusesCount;
        public fixed ushort capturedBonusesAt[3];

        public fixed ushort captureCount[6];
        public const int PLAYER_CAPTURE_CAPACITY = 31 * 31;
        public fixed ushort capture[PLAYER_CAPTURE_CAPACITY * 6];

        public fixed ushort queue[31 * 31];
        public fixed uint used[31 * 31];
        public uint usedGen;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            fixed (UnsafeCapture* that = &this)
            {
                that->captureGen += CAPTURE_GEN_INCREMENT;
                for (var i = 0; i < 6; i++)
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

        public void ApplyTo(UnsafeState* state, ushort* tickScores, UnsafeUndo* undo)
        {
            fixed (UnsafeCapture* that = &this)
            {
                var ps = (UnsafePlayer*)state->players;
                var p = ps;
                for (int player = 0; player < 6; player++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    for (int i = 0; i < that->captureCount[player]; i++)
                    {
                        var v = that->capture[PLAYER_CAPTURE_CAPACITY * player + i];
                        var mask = that->captureMask[v] & CAPTURE_GEN_MASK;
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
                                if (undo != null)
                                    undo->BeforeTerritoryChange(state);

                                state->territory[v] = (byte)(state->territory[v] & ~UnsafeState.TERRITORY_OWNER_MASK | player);
                            }
                        }
                    }
                }
            }
        }

        public void Capture(UnsafeState* state, int player)
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

                for (ushort x = 0; x < 31; x++)
                {
                    var c = x;
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }

                    c = (ushort)(x + 30 * 31);
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }
                }

                for (ushort y = 1; y < 30; y++)
                {
                    var c = (ushort)(y * 31);
                    if ((state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player
                        && (state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) != player << UnsafeState.TERRITORY_LINE_SHIFT)
                    {
                        that->queue[head++] = c;
                        that->used[c] = that->usedGen;
                    }

                    c = (ushort)(30 + y * 31);
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

                for (ushort c = 0; c < 31 * 31; c++)
                {
                    if (that->used[c] != usedGen && (state->territory[c] & UnsafeState.TERRITORY_OWNER_MASK) != player)
                    {
                        var mask = that->captureMask[c];
                        if ((mask & CAPTURE_GEN_MASK) == that->captureGen)
                        {
                            if ((mask & (1 << player)) == 0)
                            {
                                that->captureMask[c] = (uint)((1 << player) | mask);
                                that->capture[player * PLAYER_CAPTURE_CAPACITY + that->captureCount[player]++] = c;
                            }
                        }
                        else
                        {
                            that->captureMask[c] = (uint)((1 << player) | that->captureGen);
                            that->captureCount[player]++;
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