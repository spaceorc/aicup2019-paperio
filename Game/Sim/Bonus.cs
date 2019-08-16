using System.Runtime.CompilerServices;
using Game.Protocol;

namespace Game.Sim
{
    public class Bonus
    {
        public BonusType type;
        public ushort pos;
        public int? active_ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ActiveTicks(int player)
        {
            return active_ticks ??
                   (type == BonusType.N ? DefaultNitroActiveTicks(player)
                       : type == BonusType.S ? DefaultSlowActiveTicks(player)
                       : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DefaultNitroActiveTicks(int player)
        {
            return player == 0 ? 10 : 50;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DefaultSlowActiveTicks(int player)
        {
            return player == 0 ? 50 : 10;
        }

        public Bonus(RequestInput.BonusData inputBonusData)
        {
            type = inputBonusData.type;
            pos = inputBonusData.position.ToCellCoords().ToCoord();
            active_ticks = inputBonusData.active_ticks;
        }
    }
}