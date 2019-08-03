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
                @"{""players"":{""1"":{""score"":0,""direction"":""down"",""territory"":[[255,405],[255,435],[255,465],[285,405],[285,435],[285,465],[315,405],[315,435],[315,465]],""position"":[315,495],""lines"":[[285,495],[285,525],[255,525],[225,525],[195,525],[165,525],[135,525],[105,525],[75,525],[45,525],[45,495],[45,465],[45,435],[45,405],[45,375],[45,345],[45,315],[45,285],[45,255],[45,225],[75,225],[105,225],[135,225],[165,225],[195,225],[225,225],[255,225],[285,225],[315,225],[345,225],[345,255],[345,285],[375,285],[375,315],[375,345],[375,375],[375,405],[375,435],[375,465],[375,495],[375,525],[345,525],[315,525],[315,495]],""bonuses"":[]},""i"":{""score"":0,""direction"":""down"",""territory"":[[555,405],[555,435],[555,465],[585,405],[585,435],[585,465],[615,405],[615,435],[615,465]],""position"":[555,495],""lines"":[[585,495],[585,525],[615,525],[645,525],[675,525],[705,525],[735,525],[765,525],[765,495],[765,465],[765,435],[765,405],[765,375],[765,345],[765,315],[765,285],[765,255],[765,225],[765,195],[735,195],[735,165],[735,135],[705,135],[675,135],[645,135],[615,135],[585,135],[555,135],[555,165],[555,195],[555,225],[555,255],[555,285],[555,315],[525,315],[525,345],[525,375],[525,405],[525,435],[525,465],[525,495],[525,525],[555,525],[555,495]],""bonuses"":[]}},""bonuses"":[],""tick_num"":271}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(10000), new Random(165574887));

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

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (1)"));

            var visioInfo = (JArray)visio["visio_info"];
            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == "319");

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));

            input.players["i"] = input.players["4"];
            //input.players["1"] = input.players["5"];
            input.players.Remove("4"); 
            //input.players.Remove("5"); 

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(2729), new Random(-1614983808));

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