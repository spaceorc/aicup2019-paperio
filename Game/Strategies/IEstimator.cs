using Game.Fast;

namespace Game.Strategies
{
    public interface IEstimator
    {
        void Before(FastState state, int player);
        double Estimate(FastState state, int player);
    }
}