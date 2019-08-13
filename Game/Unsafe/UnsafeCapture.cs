using System.Runtime.InteropServices;

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

        public fixed ushort captureCount[6];
        public fixed ushort capture[31 * 31 * 6];
        public ushort captureTotalCount;

        public fixed ushort queue[31 * 31];
        public fixed uint used[31 * 31];
        public uint usedGen;

        public void Clear()
        {
            fixed (UnsafeCapture* that = &this)
            {
                that->captureTotalCount = 0;
                that->captureGen += CAPTURE_GEN_INCREMENT;
                for (var i = 0; i < 6; i++)
                    that->captureCount[i] = 0;
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
                                that->captureCount[player]++;
                                that->capture[that->captureTotalCount++] = c;
                            }
                        }
                        else
                        {
                            that->captureMask[c] = (uint)((1 << player) | that->captureGen);
                            that->captureCount[player]++;
                            that->capture[that->captureTotalCount++] = c;
                        }

                        if ((state->territory[c] & UnsafeState.TERRITORY_LINE_MASK) == player << UnsafeState.TERRITORY_LINE_SHIFT)
                            state->territory[c] = (byte)(state->territory[c] | UnsafeState.TERRITORY_LINE_NO);
                    }
                }
            }
        }
    }
}