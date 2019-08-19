using Game.Sim;

namespace Game.Strategies.RandomWalk.PathEstimators
{
    public interface IPathEstimator
    {
        void Before(State state, int player);
        double Estimate(State state, InterestingFacts facts, int player, int pathStartLen);
    }
}