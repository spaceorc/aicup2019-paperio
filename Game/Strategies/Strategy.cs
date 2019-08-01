using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class Strategy : IStrategy
    {
        private readonly Config config;
        private readonly IAi ai;
        private readonly FastState state = new FastState();
        
        public Strategy(Config config, IAi ai)
        {
            this.config = config;
            this.ai = ai;
        }

        public RequestOutput OnTick(RequestInput requestInput, TimeManager timeManager)
        {
            state.SetInput(config, requestInput);
            var seed = Guid.NewGuid().GetHashCode();
            Logger.Debug($"seed: {seed}");
            try
            {
                var direction = ai.GetCommand(state, state.curPlayer, timeManager, new Random(seed));
                return new RequestOutput {Command = direction, Debug = $"seed: {seed}"};
            }
            catch (Exception e)
            {
                var direction = default(Direction);
                var curDir = state.players[state.curPlayer].dir;
                if (curDir != null)
                {
                    for (int d = 1; d <= 3; d++)
                    {
                        var nd = (Direction)(((int)curDir.Value + d) % 4);
                        var next = state.NextCoord(state.players[state.curPlayer].arrivePos, nd);
                        if (next != ushort.MaxValue)
                        {
                            direction = nd;
                            if (state.territory[next] == state.curPlayer)
                                break;
                        }
                    }
                }
                return new RequestOutput {Command = direction, Debug = $"seed: {seed}", Error = e.ToString()};
            }
        }
    }
}