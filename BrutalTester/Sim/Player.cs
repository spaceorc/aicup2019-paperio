using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace BrutalTester.Sim
{
    public class Player
    {
        public int Id { get; }
        public V Pos { get; private set; }
        public int Speed { get; set; }
        public Direction Direction { get; private set; }
        public List<V> Lines { get; } = new List<V>();
        public Territory Territory { get; }
        public List<Bonus> Bonuses { get; } = new List<Bonus>();
        public int Score { get; set; }
        public IClient Client { get; }

        public Player(int id, V pos, IClient client)
        {
            Id = id;
            Pos = pos;
            Client = client;
            Speed = Env.SPEED;
            Direction = Direction.Left;
            Territory = new Territory(pos);
        }

        public V GetShift(int d) =>
            Direction == Direction.Up ? new V(0, d)
            : Direction == Direction.Down ? new V(0, -d)
            : Direction == Direction.Right ? new V(d, 0)
            : new V(-d, 0);

        public void Move()
        {
            Pos += GetShift(Speed);
        }

        public void ChangeDirection(Direction direction)
        {
            if (direction == Direction.Up && Direction != Direction.Down)
                Direction = Direction.Up;
            if (direction == Direction.Down && Direction != Direction.Up)
                Direction = Direction.Down;
            if (direction == Direction.Left && Direction != Direction.Right)
                Direction = Direction.Left;
            if (direction == Direction.Right && Direction != Direction.Left)
                Direction = Direction.Right;
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

        public void UpdateLines()
        {
            if (!Territory.Points.Contains(Pos) || Lines.Count > 0)
                Lines.Add(Pos);
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
    }
}