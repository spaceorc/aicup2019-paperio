using System;
using System.Diagnostics;
using System.Linq;
using BrutalTester.Sim;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;
using Game.Types;
using Newtonsoft.Json;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void METHOD2()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;
            
            var state = new FastState();

            var config = new Config
            {
                x_cells_count = 31,
                y_cells_count = 31,
                speed = 5,
                width = 30
            };
            config.Prepare();

            var input = JsonConvert.DeserializeObject<RequestInput>(
                @"{""players"":{""2"":{""score"":277,""direction"":""right"",""territory"":[[345,495],[345,525],[345,555],[345,585],[345,615],[345,645],[345,675],[345,705],[345,735],[345,765],[345,795],[375,495],[375,525],[375,555],[375,585],[375,615],[375,645],[375,675],[375,705],[375,735],[375,765],[375,795],[405,495],[405,525],[405,555],[405,585],[405,615],[405,645],[405,675],[405,705],[405,735],[405,765],[405,795],[435,495],[435,525],[435,555],[435,585],[435,615],[435,645],[435,675],[435,705],[435,735],[435,765],[435,795],[465,495],[465,525],[465,555],[465,585],[465,615],[465,645],[465,675],[465,705],[465,735],[465,765],[465,795],[495,375],[495,405],[495,435],[495,465],[495,495],[495,525],[495,555],[495,585],[495,615],[495,645],[495,675],[495,705],[495,735],[495,765],[495,795],[525,375],[525,405],[525,435],[525,465],[525,495],[525,525],[525,555],[525,585],[525,615],[525,645],[525,675],[525,705],[525,735],[525,765],[525,795],[555,375],[555,405],[555,435],[555,465],[555,495],[555,525],[555,555],[555,585],[555,615],[555,645],[555,675],[555,705],[555,735],[555,765],[555,795],[585,375],[585,405],[585,435],[585,465],[585,495],[585,525],[585,555],[585,585],[585,615],[585,645],[585,675],[585,705],[585,735],[585,765],[585,795],[615,375],[615,405],[615,435],[615,465],[615,495],[615,525],[615,555],[615,585],[615,615],[615,645],[615,675],[615,705],[615,735],[615,765],[615,795],[645,405],[645,435],[645,465],[645,495],[645,525],[645,555],[645,585],[645,615],[645,645],[645,675],[645,705],[645,735],[645,765],[645,795],[675,405],[675,435],[675,465],[675,495],[675,525],[675,555],[675,585],[675,615],[675,645],[675,675],[675,705],[675,735],[675,765],[675,795],[705,435],[705,465],[705,495],[705,525],[705,555],[705,585],[705,615],[705,645],[705,675],[705,705],[705,735],[705,765],[705,795],[735,435],[735,465],[735,495],[735,525],[735,555],[735,585],[735,615],[735,645],[735,675],[735,705],[735,735],[735,765],[735,795],[765,495],[765,525],[765,555],[765,585],[765,615],[765,645],[765,675],[765,705],[765,735],[765,765],[765,795]],""position"":[765,285],""lines"":[[465,465],[465,435],[435,435],[405,435],[405,405],[405,375],[405,345],[405,315],[405,285],[435,285],[465,285],[495,285],[495,255],[525,255],[555,255],[585,255],[615,255],[615,285],[645,285],[675,285],[705,285],[735,285],[765,285]],""bonuses"":[]},""i"":{""score"":140,""direction"":""down"",""territory"":[[105,525],[105,555],[135,375],[135,405],[135,435],[135,465],[135,495],[135,525],[135,555],[165,375],[165,405],[165,435],[165,465],[165,495],[165,525],[165,555],[165,585],[195,375],[195,405],[195,435],[195,465],[195,495],[195,525],[195,555],[195,585],[225,375],[225,405],[225,435],[225,465],[225,495],[225,525],[225,555],[255,375],[255,405],[255,435],[255,465],[255,495],[255,525],[255,555],[255,585],[255,615],[285,375],[285,405],[285,435],[285,465],[285,495],[285,525],[285,555],[285,585],[285,615],[315,345],[315,375],[315,405],[315,435],[315,465],[315,495],[315,525],[315,555],[315,585],[315,615],[345,345],[345,375],[345,405],[345,435],[345,465],[375,345],[375,375],[375,405],[375,435],[375,465],[405,345],[405,375],[405,405],[405,435],[405,465],[435,345],[435,375],[435,405],[435,435],[435,465],[465,345],[465,375],[465,405],[465,435],[465,465]],""position"":[585,525],""lines"":[[405,495],[375,495],[375,525],[375,555],[375,585],[345,585],[345,615],[375,615],[375,645],[375,675],[375,705],[405,705],[435,705],[435,675],[435,645],[435,615],[435,585],[465,585],[495,585],[525,585],[555,585],[585,585],[585,555],[585,525]],""bonuses"":[]}},""bonuses"":[{""type"":""saw"",""position"":[465,45]},{""type"":""saw"",""position"":[795,705]}],""tick_num"":559}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new SimpleAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000), new Random(8902443));

            Console.Out.WriteLine(command?.ToString() ?? "NULL");
        }

        private class TestingTimeManager : ITimeManager
        {
            private int counter;

            public TestingTimeManager(int counter)
            {
                this.counter = counter;
            }

            public bool IsExpired => counter-- <= 0;
            public bool BeStupid { get; }
            public bool BeSmart { get; }
            public bool IsExpiredGlobal { get; }
        }

        [Test]
        public void METHOD()
        {
            var state = new FastState();

            var clients = Enumerable.Range(0, 6).Select(x => new TestClient()).ToArray();

            var game = new BrutalTester.Sim.Game(clients);
            game.SendGameStart();
            game.GameLoop();

            var config = clients[0].Config;
            state.SetInput(config, clients[0].Input);

            var commands = new Direction[state.players.Length];
            var undo = state.NextTurn(commands, true);
            state.Undo(undo);

            var stopwatch = Stopwatch.StartNew();
            long counter = 0;

            while (stopwatch.ElapsedMilliseconds < 4000)
            {
                counter++;
                // for (int i = 0; i < state.players.Length; i++)
                // {
                //     if (state.players[i].status == PlayerStatus.Active && state.players[i].arriveTime == 0)
                //     {
                //         if (state.players[i].dir == null)
                //             commands[i] = Direction.Up;
                //         else
                //             commands[i] = (Direction)(((int)state.players[i].dir.Value + 1) % 4);
                //     }
                // }

                undo = state.NextTurn(commands, true);
                state.Undo(undo);
            }

            Console.Out.WriteLine(counter * 6);
        }

        private class TestClient : IClient
        {
            public Config Config { get; private set; }
            public RequestInput Input { get; private set; }

            public void SendConfig(Config config)
            {
                Config = config;
                config.Prepare();
            }

            public void SendGameEnd()
            {
            }

            public RequestOutput SendRequestInput(RequestInput requestInput)
            {
                Input = requestInput;
                return new RequestOutput();
            }
        }
    }
}