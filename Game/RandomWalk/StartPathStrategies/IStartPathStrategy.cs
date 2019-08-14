using Game.Protocol;
using Game.Sim;

namespace Game.RandomWalk.StartPathStrategies
{
    public interface IStartPathStrategy
    {
        RequestOutput GotoStart(State state, int player, DistanceMapGenerator distanceMap);
    }
}