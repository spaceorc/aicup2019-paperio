using System.Linq;
using Game.Protocol;
using Game.Types;

namespace BrutalTester.Sim
{
    public class Slowdown : Bonus
    {
        public Slowdown(V pos)
            : base(pos)
        {
        }

        public override BonusType Type => BonusType.S;

        public override void Apply(Player player)
        {
            var b = player.Bonuses.OfType<Slowdown>().SingleOrDefault();
            if (b != null)
                b.ActiveTicks += ActiveTicks;
            else
            {
                player.Bonuses.Add(this);
                while (player.Speed > 1)
                {
                    player.Speed--;
                    if (Env.WIDTH % player.Speed == 0)
                        break;
                }
            }
        }

        public override void Cancel(Player player)
        {
            while (player.Speed < Env.WIDTH)
            {
                player.Speed++;
                if (Env.WIDTH % player.Speed == 0)
                    break;
            }
        }
    }
}