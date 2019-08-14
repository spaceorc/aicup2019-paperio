using Game.Protocol;

namespace Game.Sim
{
    public class Bonus
    {
        public BonusType type;
        public ushort pos;

        public Bonus(State state, RequestInput.BonusData inputBonusData)
        {
            type = inputBonusData.type;
            pos = state.ToCoord(inputBonusData.position.ToCellCoords(Env.WIDTH));
        }
    }
}