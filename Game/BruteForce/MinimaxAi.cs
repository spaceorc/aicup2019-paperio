using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;

namespace Game.BruteForce
{
    public class MinimaxAi : IAi
    {
        private readonly Minimax minimax;

        public MinimaxAi(int maxDepth)
        {
            minimax = new Minimax(new MinimaxEstimator(), maxDepth);
        }
        
        public RequestOutput GetCommand(FastState state, int player, ITimeManager timeManager, Random random)
        {
            minimax.Alphabeta(timeManager, state, player);
            return new RequestOutput
            {
                Command = minimax.bestAction, 
                Debug = $"Estimations: {minimax.estimations}. BestDepth: {minimax.bestDepth}. BestScore: {minimax.bestScore}"
            };
        }
    }
}