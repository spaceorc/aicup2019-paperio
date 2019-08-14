using Game.Protocol;

namespace BrutalTester.Sim
{
    public class Saw : Bonus
    {
        public Saw(V pos)
            : base(pos)
        {
        }

        public override BonusType Type => BonusType.Saw;

        public override void Apply(Player player)
        {
            ActiveTicks = 0;
            player.Bonuses.Add(this);
        }

        public override void Cancel(Player player)
        {
        }
    }
}