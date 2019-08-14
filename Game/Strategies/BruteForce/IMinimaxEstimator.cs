using Game.Sim;

namespace Game.Strategies.BruteForce
{
    public interface IMinimaxEstimator
    {
        double Estimate(State state, int player);
    }
}