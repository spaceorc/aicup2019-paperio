using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BrutalTester.Sim;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Strategies;
using Game.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void Debug()
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
                @"{""players"":{""1"":{""score"":167,""direction"":""left"",""territory"":[[15,195],[15,225],[15,255],[15,285],[15,315],[15,345],[15,375],[15,405],[15,435],[15,465],[15,495],[15,525],[45,105],[45,135],[45,165],[45,195],[45,225],[45,255],[45,285],[45,315],[45,345],[45,375],[45,405],[45,435],[45,465],[45,495],[45,525],[75,105],[75,135],[75,165],[75,195],[75,225],[75,255],[75,285],[75,315],[75,345],[75,375],[75,405],[75,435],[75,465],[75,495],[75,525],[105,105],[105,135],[105,165],[105,195],[105,225],[105,255],[105,285],[105,315],[105,345],[105,375],[105,405],[105,435],[105,465],[105,495],[105,525],[135,105],[135,135],[135,165],[135,195],[135,225],[135,255],[135,285],[135,315],[135,345],[135,375],[135,405],[135,435],[135,465],[135,495],[135,525],[165,105],[165,135],[165,165],[165,195],[165,225],[165,255],[165,285],[165,315],[165,345],[165,375],[165,405],[165,435],[165,465],[165,495],[165,525],[195,105],[195,135],[195,165],[195,195],[195,225],[195,255],[195,285],[195,315],[195,345],[195,375],[195,405],[195,435],[195,465],[195,495],[195,525],[225,105],[225,135],[225,165],[225,195],[225,225],[225,255],[225,285],[225,315],[225,345],[225,375],[225,405],[225,435],[225,465],[225,495],[225,525],[255,105],[255,135],[255,165],[255,195],[255,225],[255,255],[255,285],[255,315],[255,345],[255,375],[255,405],[255,435],[255,465],[255,495],[255,525],[285,105],[285,135],[285,165],[285,195],[285,225],[285,255],[285,285],[285,315],[285,345],[285,375],[285,405],[285,435],[285,465],[285,495],[285,525],[315,105],[315,135],[315,165],[315,195],[315,225],[315,255],[315,285],[315,315],[315,345],[315,375],[315,405],[315,435],[315,465],[315,495],[315,525],[345,255],[345,285],[345,315],[345,345],[345,375],[345,405],[345,435],[345,465],[345,495],[375,375],[375,405],[375,435],[375,465],[375,495]],""position"":[105,915],""lines"":[[315,555],[315,585],[285,585],[255,585],[255,615],[225,615],[225,645],[195,645],[195,675],[195,705],[195,735],[195,765],[195,795],[165,795],[165,825],[135,825],[135,855],[135,885],[135,915],[105,915]],""bonuses"":[]},""i"":{""score"":174,""direction"":""right"",""territory"":[[495,345],[495,375],[495,405],[495,435],[495,465],[495,495],[525,255],[525,285],[525,315],[525,345],[525,375],[525,405],[525,435],[525,465],[525,495],[555,165],[555,195],[555,225],[555,255],[555,285],[555,315],[555,345],[555,375],[555,405],[555,435],[555,465],[555,495],[555,525],[555,555],[585,165],[585,195],[585,225],[585,255],[585,285],[585,315],[585,345],[585,375],[585,405],[585,435],[585,465],[585,495],[585,525],[585,555],[615,165],[615,195],[615,225],[615,255],[615,285],[615,315],[615,345],[615,375],[615,405],[615,435],[615,465],[615,495],[615,525],[615,555],[645,165],[645,195],[645,225],[645,255],[645,285],[645,315],[645,345],[645,375],[645,405],[645,435],[645,465],[645,495],[645,525],[645,555],[675,165],[675,195],[675,225],[675,255],[675,285],[675,315],[675,345],[675,375],[675,405],[675,435],[675,465],[675,495],[675,525],[675,555],[705,165],[705,195],[705,225],[705,255],[705,285],[705,315],[705,345],[705,375],[705,405],[705,435],[705,465],[705,495],[705,525],[705,555],[735,165],[735,195],[735,225],[735,255],[735,285],[735,315],[735,345],[735,375],[735,405],[735,435],[735,465],[735,495],[735,525],[735,555],[765,165],[765,195],[765,225],[765,255],[765,285],[765,315],[765,345],[765,375],[765,405],[765,435],[765,465],[765,495],[765,525],[765,555],[795,165],[795,195],[795,225],[795,255],[795,285],[795,315],[795,345],[795,375],[795,405],[795,435],[795,465],[795,495],[795,525],[795,555],[825,165],[825,195],[825,225],[825,255],[825,285],[825,315],[825,345],[825,375],[825,405],[825,435],[825,465],[825,495],[825,525],[825,555],[855,165],[855,195],[855,225],[855,255],[855,285],[855,315],[855,345],[855,375],[855,405],[855,435],[855,465],[855,495],[855,525],[855,555],[885,165],[885,195],[885,225],[885,255],[885,285],[885,315],[885,345],[885,375],[885,405],[885,435],[885,465],[885,495],[885,525],[885,555]],""position"":[765,855],""lines"":[[525,525],[525,555],[525,585],[525,615],[525,645],[555,645],[555,675],[585,675],[615,675],[645,675],[675,675],[675,705],[675,735],[675,765],[675,795],[705,795],[705,825],[705,855],[735,855],[765,855]],""bonuses"":[]}},""bonuses"":[],""tick_num"":469}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(10000), new Random(-317180325));

            Console.Out.WriteLine(command.ToJson());
        }
        
        [Test]
        public void Debug_visio()
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

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (3)"));

            var visioInfo = (JArray)visio["visio_info"];
            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == "991");

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));

            input.players["i"] = input.players["6"];
            //input.players["1"] = input.players["5"];
            input.players.Remove("6"); 
            //input.players.Remove("5"); 

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(2729), new Random(1186199231));

            Console.Out.WriteLine(command.ToJson());
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