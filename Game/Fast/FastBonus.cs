using Game.Protocol;

namespace Game.Fast
{
    public class FastBonus
    {
        public BonusType type;
        public ushort pos;

        public FastBonus(FastState state, RequestInput.BonusData inputBonusData)
        {
            type = inputBonusData.type;
            pos = state.ToCoord(inputBonusData.position.ToCellCoords(Env.WIDTH));
        }
    }
}