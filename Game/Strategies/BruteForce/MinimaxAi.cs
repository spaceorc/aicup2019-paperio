using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.BruteForce
{
    public class MinimaxAi : IAi
    {
        private readonly Minimax minimax;

        public MinimaxAi(int maxDepth)
        {
            minimax = new Minimax(new MinimaxEstimator(), maxDepth);
        }
        
        public RequestOutput GetCommand(State state, int player, ITimeManager timeManager, Random random)
        {
            minimax.Alphabeta(timeManager, state, player, null, null, null);
            return new RequestOutput
            {
                Command = minimax.bestAction, 
                Debug = $"Estimations: {minimax.estimations}. BestDepth: {minimax.bestDepth}. BestScore: {minimax.bestScore}"
            };
        }
    }
}