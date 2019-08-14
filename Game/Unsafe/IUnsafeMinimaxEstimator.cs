namespace Game.Unsafe
{
    public unsafe interface IUnsafeMinimaxEstimator
    {
        double Estimate(UnsafeState* state, int player);
    }
}