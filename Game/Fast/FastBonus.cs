using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastBonus
    {
        public BonusType type;
        public V pos;

        public FastBonus(RequestInput.BonusData inputBonusData, Config config)
        {
            type = inputBonusData.type;
            pos = inputBonusData.position.ToCellCoords(config.width);
        }
    }
}