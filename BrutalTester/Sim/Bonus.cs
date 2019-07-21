using System.Collections.Generic;
using Game.Protocol;
using Game.Types;

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

        private static int GenerateActiveTicks()
        {
            return Helpers.Rand(6) * 10;
        }

        public V Pos { get; }
        public int Tick { get; set; }
        public int ActiveTicks { get; set; }
        public int RemainingTicks => ActiveTicks - Tick;

        public bool IsAte(Player player, HashSet<V> captured)
        {
            return Pos == player.Pos || captured.Contains(Pos);
        }

        public abstract BonusType Type { get; }
        public abstract void Apply(Player player);
        public abstract void Cancel(Player player);

        public static V GenerateCoordinates(List<Player> players, HashSet<V> busyPoints)
        {
            var v = Helpers.GetRandomCoordinates();
            while (!IsAvailablePoint(v, players, busyPoints))
                v = Helpers.GetRandomCoordinates();
            return v;

        }

        private static bool IsAvailablePoint(V v, List<Player> players, HashSet<V> busyPoints)
        {
            if (busyPoints.Contains(v))
                return false;
            
            foreach (var p in players)
            {
                if (p.Pos.IntersectsWith(v, Env.WIDTH * 2))
                    return false;
            }

            return true;
        }
    }
}