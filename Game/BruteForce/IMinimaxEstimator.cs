using Game.Sim;

namespace Game.BruteForce
{
    public interface IMinimaxEstimator
    {
        double Estimate(State state, int player);
    }
}