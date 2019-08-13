using Game.Fast;

namespace Game.BruteForce
{
    public interface IMinimaxEstimator
    {
        double Estimate(FastState state, int player);
    }
}