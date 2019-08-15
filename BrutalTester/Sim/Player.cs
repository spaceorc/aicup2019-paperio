using System.Collections.Generic;
using System.Linq;
using Game.Protocol;

namespace BrutalTester.Sim
{
    public class Player
    {
        public int Id { get; }
        public V Pos { get; private set; }
        public int Speed { get; set; }
        public Direction? Dir { get; private set; }
        public List<V> Lines { get; } = new List<V>();
        public Territory Territory { get; }
        public List<Bonus> Bonuses { get; } = new List<Bonus>();
        public int Score { get; set; }
        public int TickScore { get; set; }
        public IClient Client { get; }

        public Player(int id, V pos, IClient client)
        {
            Id = id;
            Pos = pos;
            Client = client;
            Speed = Env.SPEED;
            Dir = null;
            Territory = new Territory(pos);
        }

        public void ChangeDirection(Direction command)
        {
            if (command == Direction.Up && Dir != Direction.Down)
                Dir = Direction.Up;
            if (command == Direction.Down && Dir != Direction.Up)
                Dir = Direction.Down;
            if (command == Direction.Left && Dir != Direction.Right)
                Dir = Direction.Left;
            if (command == Direction.Right && Dir != Direction.Left)
                Dir = Direction.Right;
        }

        public void Move()
        {
            Pos += GetShift(Speed);
        }

        public void UpdateLines()
        {
            if (!Territory.Points.Contains(Pos) || Lines.Count > 0)
                Lines.Add(Pos);
        }

        public void RemoveSawBonus()
        {
            foreach (var bonus in Bonuses.ToList())
            {
                if (bonus is Saw)
                {
                    bonus.Cancel(this);
                    Bonuses.Remove(bonus);
                }
            }
        }

        public void TickAction()
        {
            foreach (var bonus in Bonuses.ToList())
            {
                bonus.Tick++;
                if (bonus.Tick >= bonus.ActiveTicks)
                {
                    bonus.Cancel(this);
                    Bonuses.Remove(bonus);
                }
            }
        }
        
        public static V DiffPosition(Direction direction, V pos, int val)
        {
            return pos - V.vertAndHoriz[(int)direction] * val;
        }

        public (V pos, bool isMove) GetPosition()
        {
            if (Dir == null)
                return (Pos, false);

            var v = Pos;
            while (!v.InCellCenter())
            {
                v = DiffPosition(Dir.Value, v, Speed);
            }

            return (v, v != Pos);
        }

        public V GetPrevPosition()
        {
            if (Dir == null)
                return Pos;
            return DiffPosition(Dir.Value, Pos, Env.WIDTH);
        }

        public List<V> GetDirectionLine()
        {
            var points = new List<V>();
            var p = Pos;
            var shift = GetShift(Env.WIDTH);
            while (p.X > 0 && p.Y > 0 && p.X < Env.WINDOW_WIDTH && p.Y < Env.WINDOW_HEIGHT)
            {
                p += shift;
                points.Add(p);
            }

            return points;
        }

        private V GetShift(int d) =>
            Dir == Direction.Up ? V.Get(0, d)
            : Dir == Direction.Down ? V.Get(0, -d)
            : Dir == Direction.Right ? V.Get(d, 0)
            : Dir == Direction.Left ? V.Get(-d, 0)
            : V.Zero;

        public bool IsAte(Dictionary<Player, HashSet<V>> playerToCaptured)
        {
            foreach (var kvp in playerToCaptured)
            {
                var p = kvp.Key;
                var captured = kvp.Value;
                var (position, isMove) = GetPosition();
                if (this != p && captured.Contains(position) && (isMove || captured.Contains(GetPrevPosition())))
                    return true;
            }
            return false;
        }
    }
}