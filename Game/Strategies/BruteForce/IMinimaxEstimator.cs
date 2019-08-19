using Game.Sim;
using Game.Strategies.RandomWalk;

namespace Game.Strategies.BruteForce
{
    public interface IMinimaxEstimator
    {
        double Estimate(State state, int player, InterestingFacts facts);
    }
}