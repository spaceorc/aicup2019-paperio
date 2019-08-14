using Game.Sim;

namespace Game.Strategies
{
    public interface IEstimator
    {
        void Before(State state, int player);
        double Estimate(State state, int player, int pathStartLen);
    }
}