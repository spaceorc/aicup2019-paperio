using Game.Sim;

namespace Game.RandomWalk.PathEstimators
{
    public interface IPathEstimator
    {
        void Before(State state, int player);
        double Estimate(State state, int player, int pathStartLen);
    }
}