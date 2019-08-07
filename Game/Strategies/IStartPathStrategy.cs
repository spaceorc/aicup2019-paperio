using Game.Fast;
using Game.Protocol;

namespace Game.Strategies
{
    public interface IStartPathStrategy
    {
        RequestOutput GotoStart(FastState state, int player, DistanceMapGenerator distanceMap);
    }
}