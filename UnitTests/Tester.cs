using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Strategies;
using Game.Strategies.BruteForce;
using Game.Strategies.RandomWalk;
using Game.Strategies.RandomWalk.PathEstimators;
using Game.Strategies.RandomWalk.StartPathStrategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Tester
    {
        [Test]
        public void Debug()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var input = JsonConvert.DeserializeObject<RequestInput>(
                @"{""players"":{""2"":{""score"":93,""direction"":""down"",""territory"":[[225,675],[255,495],[225,645],[285,525],[285,675],[495,585],[405,525],[435,555],[195,645],[255,585],[405,555],[375,495],[225,615],[45,555],[255,555],[105,525],[165,645],[135,495],[285,585],[135,645],[165,495],[315,555],[45,585],[375,525],[345,555],[135,555],[375,555],[225,585],[285,495],[345,585],[285,645],[75,525],[195,525],[165,555],[315,495],[45,495],[255,645],[105,585],[375,585],[345,495],[75,495],[225,555],[135,585],[255,615],[45,525],[105,495],[225,525],[135,615],[165,525],[285,555],[195,555],[285,705],[495,555],[345,525],[315,585],[255,705],[435,585],[75,585],[465,555],[225,495],[405,585],[195,585],[255,675],[225,705],[165,585],[285,615],[465,585],[75,555],[165,615],[135,525],[315,525],[315,615],[105,555],[255,525],[435,525],[195,615]],""position"":[285,465],""lines"":[[285,465]],""bonuses"":[]},""3"":{""score"":141,""direction"":""right"",""territory"":[[555,135],[555,405],[495,435],[465,525],[525,405],[555,435],[615,195],[555,165],[675,315],[585,465],[645,225],[645,375],[525,225],[585,315],[525,375],[615,315],[675,375],[585,285],[555,375],[645,315],[585,165],[555,345],[555,525],[495,525],[465,435],[495,375],[525,195],[525,345],[615,135],[645,345],[465,465],[585,135],[495,465],[495,315],[675,195],[645,165],[615,225],[525,525],[525,165],[555,285],[585,375],[525,315],[615,255],[675,285],[495,495],[525,435],[585,345],[645,195],[675,165],[645,255],[585,225],[525,495],[615,345],[675,255],[525,135],[555,315],[525,285],[555,495],[645,285],[555,465],[585,195],[495,405],[555,195],[525,465],[615,165],[585,435],[525,255],[675,345],[495,345],[555,255],[585,405],[675,225],[645,135],[555,225],[585,255],[615,285],[615,375],[465,495]],""position"":[495,435],""lines"":[],""bonuses"":[]},""4"":{""score"":42,""direction"":""down"",""territory"":[[675,495],[555,555],[645,465],[645,615],[645,525],[585,555],[675,435],[615,465],[615,555],[555,615],[585,525],[645,555],[555,585],[615,435],[525,585],[645,585],[585,495],[615,525],[615,615],[675,555],[585,615],[525,555],[615,495],[675,525],[585,585],[645,435],[645,495],[675,465],[615,585]],""position"":[765,465],""lines"":[[615,645],[585,645],[555,645],[525,645],[525,675],[495,675],[465,675],[465,705],[495,705],[525,705],[555,705],[585,705],[615,705],[645,705],[645,675],[645,645],[675,645],[705,645],[705,615],[705,585],[705,555],[705,525],[735,525],[765,525],[765,495],[765,465]],""bonuses"":[]},""i"":{""score"":21,""direction"":""right"",""territory"":[[315,285],[165,255],[225,345],[225,375],[255,435],[285,255],[195,225],[285,405],[195,255],[225,435],[255,375],[255,345],[315,255],[285,345],[165,225],[225,255],[255,225],[225,405],[255,285],[285,285],[315,315],[285,435],[225,315],[255,315],[285,375],[225,225],[225,285],[255,405],[255,255],[285,315]],""position"":[255,15],""lines"":[[195,345],[165,345],[135,345],[105,345],[75,345],[45,345],[15,345],[15,315],[15,285],[15,255],[15,225],[15,195],[15,165],[15,135],[15,105],[15,75],[15,45],[15,15],[45,15],[75,15],[105,15],[135,15],[165,15],[195,15],[225,15],[255,15]],""bonuses"":[]}},""bonuses"":[{""type"":""s"",""position"":[195,495]}],""tick_num"":301}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000), new Random(-741733900));

            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Debug_visio()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (22)"));

            var visioInfo = (JArray)visio["visio_info"];
            var t = 583;

            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == t.ToString());
            var prevTick = visioInfo.Single(x => x["tick_num"]?.ToString() == (t - 1).ToString());

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            var prevInput = prevTick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            foreach (var kvp in prevInput.players)
            {
                if (input.players.TryGetValue(kvp.Key, out var pp))
                    pp.direction = kvp.Value.direction;
            }

            state.SetInput(input, "1");

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000), new Random(1902244719));
            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Perf()
        {
            var state = new State();
            state.SetInput(
                new RequestInput
                {
                    tick_num = 1,
                    players = new Dictionary<string, RequestInput.PlayerData>
                    {
                        {
                            "i", new RequestInput.PlayerData
                            {
                                territory = new[] {V.Get(0, 0).ToRealCoords()},
                                lines = Enumerable.Range(1, 30)
                                    .Select(x => V.Get(x, 0).ToRealCoords())
                                    .Concat(Enumerable.Range(1, 30).Select(y => V.Get(30, y).ToRealCoords()))
                                    .Concat(Enumerable.Range(0, 30).Select(x => V.Get(29 - x, 30).ToRealCoords()))
                                    .Concat(Enumerable.Range(0, 29).Select(y => V.Get(0, 29 - y).ToRealCoords()))
                                    .ToArray(),
                                position = V.Get(0, 1).ToRealCoords(),
                                direction = Direction.Down,
                                bonuses = new RequestInput.BonusState[0]
                            }
                        }
                    },
                    bonuses = new RequestInput.BonusData[0]
                });

            Console.Out.WriteLine(state.Print());
            var commands = new[]{Direction.Down};
            var undo = state.NextTurn(commands, true);
            
            //Console.Out.WriteLine(state.Print());
            state.Undo(undo);
            
            //Console.Out.WriteLine(state.Print());

            var sw = Stopwatch.StartNew();

            var counter = 0;
            while (sw.ElapsedMilliseconds < 1000)
            {
                undo = state.NextTurn(commands, true);
                state.Undo(undo);
                counter++;
            }

            Console.Out.WriteLine(counter);
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
    }
}