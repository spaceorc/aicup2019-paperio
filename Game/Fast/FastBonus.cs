using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastBonus
    {
        public BonusType type;
        public ushort pos;

        public FastBonus(FastState state, RequestInput.BonusData inputBonusData, Config config)
        {
            type = inputBonusData.type;
            pos = state.ToCoord(inputBonusData.position.ToCellCoords(config.width));
        }
    }
}