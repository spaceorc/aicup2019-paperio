using System;
using System.Runtime.CompilerServices;
using Game.Protocol;

namespace Game.Sim
{
    public static class Coords
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort NextCoord(this ushort prev, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    var result = prev + Env.X_CELLS_COUNT;
                    if (result >= Env.CELLS_COUNT)
                        return UInt16.MaxValue;
                    return (ushort)result;

                case Direction.Left:
                    if (prev % Env.X_CELLS_COUNT == 0)
                        return UInt16.MaxValue;
                    return (ushort)(prev - 1);

                case Direction.Down:
                    result = prev - Env.X_CELLS_COUNT;
                    if (result < 0)
                        return UInt16.MaxValue;
                    return (ushort)result;

                case Direction.Right:
                    if (prev % Env.X_CELLS_COUNT == Env.X_CELLS_COUNT - 1)
                        return UInt16.MaxValue;
                    return (ushort)(prev + 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToCoord(this V v) => (ushort)(v.X + v.Y * Env.X_CELLS_COUNT);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V ToV(this ushort c) => V.Get(c % Env.X_CELLS_COUNT, c / Env.X_CELLS_COUNT);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction DirTo(this ushort prev, ushort next)
        {
            var diff = next - prev;
            if (diff == 1)
                return Direction.Right;
            if (diff == -1)
                return Direction.Left;
            if (diff == Env.X_CELLS_COUNT)
                return Direction.Up;
            if (diff == -Env.X_CELLS_COUNT)
                return Direction.Down;
            throw new InvalidOperationException($"Bad cell diff: {diff}");
        }

        public static int MDist(ushort a, ushort b)
        {
            var ax = a % Env.X_CELLS_COUNT;
            var ay = a / Env.X_CELLS_COUNT;
            var bx = b % Env.X_CELLS_COUNT;
            var by = b / Env.X_CELLS_COUNT;
            return Math.Abs(ax - bx) + Math.Abs(ay - @by);
        }
    }
}