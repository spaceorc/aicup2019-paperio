using System;
using Game.Helpers;
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

        public RequestOutput OnTick(RequestInput requestInput, TimeManager timeManager)
        {
            return new RequestOutput{Command = (Direction)random.Next(4)};
        }
    }
}