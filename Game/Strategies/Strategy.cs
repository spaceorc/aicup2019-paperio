using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies
{
    public class Strategy : IStrategy
    {
        private readonly IAi ai;
        private readonly State state = new State();
        
        public Strategy(IAi ai)
        {
            this.ai = ai;
        }

        public RequestOutput OnTick(RequestInput requestInput, TimeManager timeManager)
        {
            state.SetInput(requestInput);
            var seed = Guid.NewGuid().GetHashCode();
            Logger.Debug($"seed: {seed}");
            try
            {
                var output = ai.GetCommand(state, 0, timeManager, new Random(seed));
                output.Debug += $". Command: {output.Command}. Seed: {seed}. Elapsed: {timeManager.Elapsed}. ElapsedGlobal: {timeManager.ElapsedGlobal}";
                return output;
            }
            catch (Exception e)
            {
                #if LOCAL_RUNNER
                throw;
                #endif
                var direction = default(Direction);
                var curDir = state.players[0].dir;
                if (curDir != null)
                {
                    for (var d = 3; d <= 5; d++)
                    {
                        var nd = (Direction)(((int)curDir.Value + d) % 4);
                        var next = state.NextCoord(state.players[0].arrivePos, nd);
                        if (next != ushort.MaxValue)
                        {
                            direction = nd;
                            if (state.territory[next] == 0)
                                break;
                        }
                    }
                }
                return new RequestOutput {Command = direction, Debug = $"Failure. Seed: {seed}. Elapsed: {timeManager.Elapsed}. ElapsedGlobal: {timeManager.ElapsedGlobal}", Error = e.ToString()};
            }
        }
    }
}