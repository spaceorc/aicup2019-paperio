using System.Collections.Generic;
using Game.Protocol;

namespace BrutalTester.Sim
{
    public abstract class Bonus
    {
        protected Bonus(V pos)
        {
            Pos = pos;
            Tick = 0;
            ActiveTicks = GenerateActiveTicks();
        }

        public V Pos { get; }
        public int Tick { get; set; }
        public int ActiveTicks { get; protected set; }
        public int RemainingTicks => ActiveTicks - Tick;

        public abstract BonusType Type { get; }
        public abstract void Apply(Player player);
        public abstract void Cancel(Player player);

        private static int GenerateActiveTicks()
        {
            return Helpers.RandInt(1, 5) * 10;
        }

        private static bool IsAvailablePoint(V v, List<Player> players, HashSet<V> busyPoints)
        {
            if (busyPoints.Contains(v))
                return false;

            foreach (var p in players)
            {
                if (p.Pos.X - 2 * Env.WIDTH <= v.X && v.X <= p.Pos.X + 2 * Env.WIDTH &&
                    p.Pos.Y - 2 * Env.WIDTH <= v.Y && v.Y <= p.Pos.Y + 2 * Env.WIDTH)
                    return false;
            }

            return true;
        }

        public static V GenerateCoordinates(List<Player> players, HashSet<V> busyPoints)
        {
            var v = Helpers.GetRandomCoordinates();
            while (!IsAvailablePoint(v, players, busyPoints))
                v = Helpers.GetRandomCoordinates();
            return v;
        }

        public bool IsAte(Player player, HashSet<V> captured)
        {
            return Pos == player.Pos || captured.Contains(Pos);
        }
    }
}