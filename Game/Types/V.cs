using System;
using System.Linq;
using Game.Protocol;
using Newtonsoft.Json;

namespace Game.Types
{
    [JsonArray, JsonConverter(typeof(VConverter))]
    public class V : IEquatable<V>
    {
        private static readonly V[,] cache = new V[31 + 30, 31 + 30];
        private static readonly int cacheX = cache.GetLength(0);
        private static readonly int cacheY = cache.GetLength(1);

        static V()
        {
            for (int x = 0; x < 31; x++)
            for (int y = 0; x < 31; x++)
            {
                cache[x, y] = Get(x, y);
            }
        }

        public static V Zero = Get(0, 0);

        private static readonly V[] diagonals =
        {
            Get(1, 1),
            Get(-1, 1),
            Get(1, -1),
            Get(-1, -1),
        };

        public static readonly V[] vertAndHoriz =
        {
            Get(0, 1),
            Get(-1, 0),
            Get(0, -1),
            Get(1, 0),
        };

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

        public bool IntersectsWith(V other, int width)
        {
            return Math.Abs(X - other.X) < width && Math.Abs(Y - other.Y) < width;
        }

        public bool InCellCenter(int width)
        {
            return (X - width / 2) % width == 0 && (Y - width / 2) % width == 0;
        }

        public V ToCellCoords(int width)
        {
            return V.Get(
                (X - width / 2) / width,
                (Y - width / 2) / width);
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