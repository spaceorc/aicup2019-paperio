using Game.Types;

namespace Game.Fast
{
    public class FastBonusBackup
    {
        public BonusType type;
        public ushort pos;

        public void Backup(FastBonus bonus)
        {
            type = bonus.type;
            pos = bonus.pos;
        }

        public void Restore(FastBonus bonus)
        {
            bonus.type = type;
            bonus.pos = pos;
        }
    }
}