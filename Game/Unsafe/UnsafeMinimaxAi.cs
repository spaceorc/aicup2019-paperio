using System;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Unsafe
{
    public unsafe class UnsafeMinimaxAi : IUnsafeAi
    {
        private readonly UnsafeMinimax minimax;

        public UnsafeMinimaxAi(int maxDepth)
        {
            minimax = new UnsafeMinimax(new UnsafeMinimaxEstimator(), maxDepth);
        }
        
        public RequestOutput GetCommand(UnsafeState* state, int player, ITimeManager timeManager, Random random)
        {
            minimax.Alphabeta(timeManager, state, player);
            return new RequestOutput
            {
                Command = (Direction)minimax.bestAction, 
                Debug = $"Estimations: {minimax.estimations}. BestDepth: {minimax.bestDepth}. BestScore: {minimax.bestScore}"
            };
        }
    }
}