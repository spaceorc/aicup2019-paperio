using System;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;

namespace Game
{
    public static class main
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, eventArgs) => Logger.Error((Exception)eventArgs.ExceptionObject);

            Logger.minLevel = Logger.Level.Debug;
            Logger.Info("Waiting for config...");

            var config = ConsoleProtocol.ReadConfig();
            Logger.Info($"Config: {config.ToJson()}");

            var timeManager = new TimeManager();
            var strategy = new Strategy(config); //StrategiesRegistry.Create(Settings.DefaultStrategy, config);
            while (true)
            {
                Logger.Info("Waiting for data...");
                var data = ConsoleProtocol.ReadTurnInput();
                if (data == null)
                {
                    Logger.Info("Game is over...");
                    break;
                }
                
                timeManager.TickStarted();
                Logger.Info($"Data: {data.ToJson()}");
                var command = strategy.OnTick(data, timeManager);
                Logger.Info($"Command: {command.ToJson()}");
                timeManager.TickFinished();
                ConsoleProtocol.WriteTurnInput(command);
            }
        }
    }
}