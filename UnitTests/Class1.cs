using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BrutalTester.Sim;
using Game.AlterStaregy;
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
                @"{""players"":{""1"":{""score"":0,""direction"":""left"",""territory"":[[285,465],[285,435],[315,405],[255,435],[315,435],[285,405],[255,465],[315,465],[255,405]],""position"":[105,255],""lines"":[[345,435],[375,435],[375,405],[375,375],[375,345],[375,315],[375,285],[375,255],[345,255],[315,255],[285,255],[255,255],[225,255],[195,255],[165,255],[135,255],[105,255]],""bonuses"":[]},""i"":{""score"":0,""direction"":""up"",""territory"":[[555,465],[615,405],[555,405],[585,405],[615,435],[585,465],[615,465],[585,435],[555,435]],""position"":[645,915],""lines"":[[645,435],[645,465],[645,495],[645,525],[645,555],[645,585],[645,615],[645,645],[645,675],[645,705],[645,735],[645,765],[645,795],[645,825],[645,855],[645,885],[645,915]],""bonuses"":[]}},""bonuses"":[],""tick_num"":109}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(config, input);

            Console.Out.WriteLine(state.Print());

            var ai = new MinimaxAi(10);

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(1500000), new Random(1848632933));

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

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (20)"));

            var visioInfo = (JArray)visio["visio_info"];
            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == "343");

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));

            //input.players["i"] = input.players["4"];
            //input.players["1"] = input.players["5"];
            //input.players.Remove("4"); 
            //input.players.Remove("5"); 

            // todo https://aicups.ru/session_debug/585226/
            
            state.SetInput(config, input, "4");

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi(new NearestOpponentStartPathStrategy(), new CaptureOpponentEstimator(), walkOnTerritory: true);

            var command = ai.GetCommand(state, state.curPlayer, new TestingTimeManager(2215), new Random(493048687));

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