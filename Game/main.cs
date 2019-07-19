using System;
using System.Linq;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;
using Game.Types;

namespace Game
{
    public static class main
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, eventArgs) => Logger.Error((Exception)eventArgs.ExceptionObject);

            Logger.minLevel = Logger.Level.Debug;

            TimeManager timeManager = null;
            Strategy strategy = null;

            while (true)
            {
                Logger.Info("Waiting data");
                var readResult = ConsoleProtocol.Read();
                if (readResult == null)
                {
                    Logger.Info("Terminating");
                    break;
                }

                if (readResult.Config != null)
                {
                    Logger.Info($"Config: {readResult.Config.ToJson()}");
                    timeManager = new TimeManager(readResult.Config);
                    strategy = new Strategy(readResult.Config);
                    continue;
                }

                if (readResult.Input != null)
                {
                    if (timeManager == null)
                        throw new InvalidOperationException();

                    timeManager.RequestStarted(
                        readResult.Input.tick_num,
                        readResult.Input.players["i"].bonuses.FirstOrDefault(b => b.type == BonusType.N)?.ticks ?? 0,
                        readResult.Input.players["i"].bonuses.FirstOrDefault(b => b.type == BonusType.S)?.ticks ?? 0);

                    Logger.Info($"Input: {readResult.Input.ToJson()}");
                    var command = strategy.OnTick(readResult.Input, timeManager);
                    timeManager.RequestFinished();

                    ConsoleProtocol.Write(command);
                    continue;
                }
                
                Logger.Debug($"Unexpected type: {readResult.Type}");
            }
        }
    }
}