using System.Runtime.CompilerServices;
using Game.Protocol;

namespace Game.Sim.Undo
{
    public class BonusBackup
    {
        private BonusType type;
        private ushort pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Backup(Bonus bonus)
        {
            type = bonus.type;
            pos = bonus.pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Restore(Bonus bonus)
        {
            bonus.type = type;
            bonus.pos = pos;
        }
    }
}