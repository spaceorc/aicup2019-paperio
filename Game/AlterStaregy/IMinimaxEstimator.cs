using Game.Fast;

namespace Game.AlterStaregy
{
    public interface IMinimaxEstimator
    {
        double Estimate(FastState state, int player);
    }
}