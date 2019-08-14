using Game.Protocol;
using Game.Sim;

namespace Game.Strategies
{
    public interface IStartPathStrategy
    {
        RequestOutput GotoStart(State state, int player, DistanceMapGenerator distanceMap);
    }
}