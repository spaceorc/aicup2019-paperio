using System;
using System.Linq;
using Newtonsoft.Json;

namespace Game.Protocol
{
    [JsonArray, JsonConverter(typeof(VConverter))]
    public class V : IEquatable<V>
    {
        private static readonly V[,] cache = new V[31 + 30, 31 + 30];
        private static readonly int cacheX = cache.GetLength(0);
        private static readonly int cacheY = cache.GetLength(1);

        static V()
        {
            for (var x = -30; x < 31; x++)
            for (var y = -30; y < 31; y++)
            {
                cache[x + 30, y + 30] = new V(x, y);
            }

            Zero = Get(0, 0);
            diagonals = new[]
            {
                Get(1, 1),
                Get(-1, 1),
                Get(1, -1),
                Get(-1, -1),
            };
            vertAndHoriz = new[]
            {
                Get(0, 1),
                Get(-1, 0),
                Get(0, -1),
                Get(1, 0),
            };
        }

        public static readonly V Zero;

        private static readonly V[] diagonals;

        public static readonly V[] vertAndHoriz;

        private V(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static V Get(int x, int y)
        {
            if (x + 30 < 0 || y + 30 < 0 || x + 30 >= cacheX || y + 30 >= cacheY)
                return new V(x, y);
            return cache[x + 30, y + 30];
        }

        public int X { get; }
        public int Y { get; }

        public override string ToString() => $"{X},{Y}";

        public bool Equals(V other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((V)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator==(V left, V right) => Equals(left, right);

        public static bool operator!=(V left, V right) => !Equals(left, right);

        public static V operator+(V left, V right) => Get(left.X + right.X, left.Y + right.Y);
        public static V operator-(V left, V right) => Get(left.X - right.X, left.Y - right.Y);
        public static V operator-(V left) => Get(-left.X, -left.Y);
        public static V operator*(V left, int k) => Get(left.X * k, left.Y * k);
        public static V operator/(V left, int k) => Get(left.X / k, left.Y / k);
        public static V operator*(int k, V left) => Get(left.X * k, left.Y * k);

        public int MLen() => Math.Abs(X) + Math.Abs(Y);
        public int CLen() => Math.Max(Math.Abs(X), Math.Abs(Y));

        public bool IntersectsWith(V other)
        {
            return Math.Abs(X - other.X) < Env.WIDTH && Math.Abs(Y - other.Y) < Env.WIDTH;
        }

        public bool InCellCenter()
        {
            return (X - Env.WIDTH / 2) % Env.WIDTH == 0 && (Y - Env.WIDTH / 2) % Env.WIDTH == 0;
        }

        public V ToCellCoords()
        {
            return V.Get(
                (X - Env.WIDTH / 2) / Env.WIDTH,
                (Y - Env.WIDTH / 2) / Env.WIDTH);
        }

        public V ToRealCoords()
        {
            return V.Get(
                X * Env.WIDTH + Env.WIDTH / 2,
                Y * Env.WIDTH + Env.WIDTH / 2);
        }

        public V[] GetDiagonals(int width)
        {
            return diagonals.Select(v => this + v * width).ToArray();
        }

        public V[] GetVertAndHoriz(int width)
        {
            return vertAndHoriz.Select(v => this + v * width).ToArray();
        }

        public V[] GetNeighboring(int width)
        {
            return GetVertAndHoriz(width).Concat(GetDiagonals(width)).ToArray();
        }

        public V[] GetSelfAndNeighboring(int width)
        {
            return new[] {this}.Concat(GetVertAndHoriz(width)).Concat(GetDiagonals(width)).ToArray();
        }
    }
}