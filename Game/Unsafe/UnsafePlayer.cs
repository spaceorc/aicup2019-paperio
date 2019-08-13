using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;

namespace Game.Unsafe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct UnsafePlayer
    {
        public const int Size = 21;

        static UnsafePlayer()
        {
            if (sizeof(UnsafePlayer) != Size)
                throw new InvalidOperationException($"sizeof(UnsafePlayer) == {sizeof(UnsafePlayer)} with expected {Size}");
        }

        public const byte STATUS_ELIMINATED = 0;
        public const byte STATUS_ACTIVE = 1;
        public const byte STATUS_LOOSER = 2;
        public const byte STATUS_BROKEN = 3;
        public byte status; // 1

        public const byte DIR_NULL = 0xFF;
        public const byte DIR_UP = 0;
        public const byte DIR_LEFT = 1;
        public const byte DIR_DOWN = 2;
        public const byte DIR_RIGHT = 3;
        public byte dir; // 1 - 0xFF null

        public ushort pos; // 2
        public ushort arrivePos; // 2

        // shiftTime (0..10), arriveTime (0.10)
        public byte shiftTime; // 1
        public byte arriveTime; // 1

        public byte killedBy; // 1
        public ushort score; // 2
        public byte nitroLeft; // 1
        public byte slowLeft; // 1
        
        public byte nitrosCollected; // 1
        public byte slowsCollected; // 1
        public ushort opponentTerritoryCaptured; // 2

        public ushort territory; // 2

        public ushort lineCount; // 2
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TickAction()
        {
            if (slowLeft > 0)
                slowLeft--;
            if (nitroLeft > 0)
                nitroLeft--;
            UpdateShiftTime();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateShiftTime()
        {
            shiftTime = GetShiftTime(nitroLeft, slowLeft);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetShiftTime(int nitroLeft, int slowLeft)
        {
            if (slowLeft > 0 && nitroLeft > 0)
                return Env.TICKS_PER_REQUEST;
            if (slowLeft > 0)
                return Env.SLOW_TICKS_PER_REQUEST;
            if (nitroLeft > 0)
                return Env.NITRO_TICKS_PER_REQUEST;
            return Env.TICKS_PER_REQUEST;
        }

    }
}