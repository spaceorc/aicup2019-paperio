using System;
using Game.Protocol;
using Game.Types;

namespace BrutalTester.Sim
{
    public static class Helpers
    {
        private static readonly Random random = new Random();

        public static int Rand(int count)
        {
            lock (random)
                return random.Next(count);
        }

        public static int RandInt(int @from, int @to)
        {
            lock (random)
                return random.Next(@from, @to + 1);
        }

        public static T RandArrayItem<T>(this T[] array)
        {
            return array[Rand(array.Length)];
        }

        public static V GetRandomCoordinates()
        {
            var x = RandInt(1, Env.X_CELLS_COUNT) * Env.WIDTH - Env.WIDTH / 2;
            var y = RandInt(1, Env.Y_CELLS_COUNT) * Env.WIDTH - Env.WIDTH / 2;
            return new V(x, y);
        }
    }
}