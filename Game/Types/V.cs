using System;
using System.Linq;
using Game.Protocol;
using Newtonsoft.Json;

namespace Game.Types
{
    [JsonArray, JsonConverter(typeof(VConverter))]
    public class V : IEquatable<V>
    {
        private static readonly V[] diagonals =
        {
            new V(1, 1), 
            new V(-1, 1), 
            new V(1, -1), 
            new V(-1, -1), 
        };
        
        private static readonly V[] vertAndHoriz =
        {
            new V(0, 1), 
            new V(-1, 0), 
            new V(0, -1), 
            new V(1, 0), 
        };

        public V(int x, int y)
        {
            X = x;
            Y = y;
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
            if (obj.GetType() != this.GetType())
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

        public static V operator+(V left, V right) => new V(left.X + right.X, left.Y + right.Y);
        public static V operator-(V left, V right) => new V(left.X - right.X, left.Y - right.Y);
        public static V operator-(V left) => new V(-left.X, -left.Y);
        public static V operator*(V left, int k) => new V(left.X * k, left.Y * k);
        public static V operator/(V left, int k) => new V(left.X / k, left.Y / k);
        public static V operator*(int k, V left) => new V(left.X * k, left.Y * k);

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
            return new[]{this}.Concat(GetVertAndHoriz(width)).Concat(GetDiagonals(width)).ToArray();
        }
    }
}