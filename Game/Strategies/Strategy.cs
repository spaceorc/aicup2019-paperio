using System;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class Strategy : IStrategy
    {
        private readonly Config config;
        private readonly Random random = new Random();

        public Strategy(Config config)
        {
            this.config = config;
        }

        public TurnOutput OnTick(TurnInput turnInput, TimeManager timeManager)
        {
            return new TurnOutput{Command = (Direction)random.Next(4)};
        }
    }
}