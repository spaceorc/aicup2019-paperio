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
                @"{""players"":{""2"":{""score"":716,""direction"":""left"",""territory"":[[225,675],[375,255],[345,735],[225,375],[255,435],[225,645],[165,345],[465,195],[405,255],[285,525],[345,705],[495,585],[345,405],[285,345],[405,525],[315,765],[195,645],[555,615],[195,375],[345,615],[405,195],[465,255],[345,315],[405,465],[525,585],[135,765],[255,255],[165,645],[225,345],[465,285],[285,255],[345,345],[285,405],[165,435],[435,495],[315,555],[195,765],[315,645],[255,345],[375,225],[375,525],[435,705],[315,195],[435,405],[555,525],[195,495],[375,345],[465,435],[495,525],[465,615],[345,255],[375,555],[195,705],[375,375],[255,315],[405,735],[195,435],[345,225],[465,465],[285,315],[465,645],[165,255],[405,405],[525,525],[165,555],[315,495],[405,675],[225,555],[255,735],[135,585],[225,255],[405,345],[255,615],[165,465],[225,525],[525,495],[195,555],[135,615],[345,525],[285,705],[375,705],[435,675],[195,285],[315,585],[255,405],[315,675],[435,225],[255,705],[315,285],[375,645],[435,585],[315,225],[435,285],[375,465],[345,435],[375,675],[225,435],[255,675],[405,615],[345,195],[225,705],[165,285],[165,585],[225,405],[285,615],[165,735],[345,465],[285,285],[405,285],[315,615],[405,555],[195,615],[135,555],[465,315],[495,495],[225,735],[465,495],[345,375],[255,765],[195,345],[135,705],[405,225],[255,495],[525,615],[135,735],[285,675],[465,525],[165,375],[435,555],[165,675],[435,255],[315,705],[255,285],[315,315],[255,585],[315,405],[555,585],[435,465],[375,285],[195,735],[375,195],[465,675],[375,495],[225,615],[375,315],[255,555],[405,495],[195,675],[285,585],[345,645],[465,705],[195,405],[135,645],[165,495],[315,255],[405,435],[525,555],[135,675],[345,555],[405,705],[225,315],[225,585],[165,405],[165,705],[225,285],[285,495],[345,585],[285,645],[285,465],[345,285],[285,765],[435,435],[195,525],[555,555],[255,645],[315,345],[315,435],[375,585],[435,645],[195,255],[375,405],[495,615],[435,195],[345,495],[195,465],[375,615],[345,765],[375,435],[375,735],[405,375],[165,525],[225,465],[285,555],[465,225],[495,555],[285,375],[405,645],[165,315],[315,735],[555,495],[165,765],[315,375],[405,315],[465,555],[225,495],[405,585],[195,585],[255,375],[225,765],[195,315],[465,585],[285,435],[435,615],[285,735],[165,615],[435,315],[315,525],[255,465],[255,525],[315,465],[435,525],[345,675]],""position"":[650,765],""lines"":[[585,525],[615,525],[645,525],[645,495],[675,495],[705,495],[735,495],[765,495],[795,495],[795,525],[795,555],[795,585],[795,615],[795,645],[765,645],[735,645],[735,675],[735,705],[735,735],[735,765],[705,765],[675,765]],""bonuses"":[]},""4"":{""score"":588,""direction"":""left"",""territory"":[[765,615],[795,405],[885,795],[915,615],[705,465],[765,885],[795,615],[795,735],[855,825],[735,765],[645,555],[735,495],[705,555],[915,705],[915,405],[645,585],[705,825],[675,375],[735,465],[825,615],[795,465],[855,495],[585,735],[825,885],[825,585],[765,645],[795,675],[675,795],[765,465],[885,405],[795,795],[675,525],[915,795],[855,585],[645,495],[795,855],[735,585],[855,555],[645,525],[915,735],[855,645],[915,435],[855,465],[675,855],[705,795],[825,795],[675,585],[825,495],[885,435],[735,885],[855,435],[825,765],[675,825],[825,465],[705,855],[885,705],[615,525],[765,735],[675,555],[795,525],[765,555],[735,525],[915,825],[915,525],[735,825],[765,825],[735,555],[855,855],[645,435],[705,495],[705,765],[885,615],[735,735],[885,885],[915,765],[645,465],[915,465],[675,735],[795,585],[825,675],[735,705],[825,645],[765,585],[795,435],[885,675],[675,405],[885,585],[765,855],[795,645],[765,675],[855,525],[645,375],[915,555],[795,765],[645,735],[855,885],[645,405],[705,735],[885,495],[885,855],[765,795],[885,765],[915,645],[915,495],[645,345],[855,405],[675,765],[735,675],[675,465],[825,855],[675,495],[825,555],[795,705],[855,735],[705,525],[795,825],[825,825],[765,705],[825,525],[885,645],[675,435],[885,555],[735,855],[885,465],[765,495],[885,825],[915,585],[765,765],[795,885],[795,495],[735,795],[855,795],[855,615],[885,735],[915,675],[705,585],[855,705],[675,345],[825,735],[855,765],[825,435],[855,675],[795,555],[765,525],[615,735],[825,705],[825,405],[885,525]],""position"":[852,825],""lines"":[],""bonuses"":[{""type"":""s"",""ticks"":14}]},""i"":{""score"":648,""direction"":""down"",""territory"":[[555,135],[525,405],[585,255],[495,135],[645,225],[525,225],[525,375],[585,315],[495,225],[585,285],[555,345],[255,225],[495,375],[615,495],[405,135],[225,225],[585,135],[495,315],[525,165],[525,315],[585,375],[495,165],[585,345],[675,255],[615,435],[555,315],[435,375],[555,465],[435,135],[585,495],[585,195],[555,195],[585,435],[465,165],[495,345],[555,255],[585,405],[525,435],[615,375],[555,405],[285,225],[465,345],[615,465],[495,435],[555,435],[615,195],[375,165],[585,525],[555,165],[255,165],[495,285],[585,465],[465,135],[615,315],[555,375],[405,165],[645,315],[585,165],[615,405],[525,195],[525,345],[615,135],[285,195],[495,465],[615,225],[555,285],[435,345],[615,255],[465,375],[675,285],[225,195],[645,255],[585,225],[345,165],[615,345],[225,165],[465,405],[525,135],[525,285],[645,285],[495,405],[495,255],[525,465],[615,165],[525,255],[285,165],[495,195],[675,225],[435,165],[555,225],[315,165],[255,195],[615,285],[675,315]],""position"":[165,525],""lines"":[[405,345],[375,345],[375,375],[345,375],[315,375],[285,375],[285,405],[285,435],[285,465],[285,495],[285,525],[255,525],[255,555],[255,585],[255,615],[225,615],[195,615],[165,615],[135,615],[135,585],[135,555],[165,555],[165,525]],""bonuses"":[{""type"":""n"",""ticks"":49}]}},""bonuses"":[{""type"":""s"",""position"":[885,75]}],""tick_num"":1008}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(67367), new Random(1883783533));

            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Debug_visio()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (23)"));

            var visioInfo = (JArray)visio["visio_info"];
            var t = 13;

            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == t.ToString());
            var prevTick = visioInfo.Single(x => x["tick_num"]?.ToString() == (t - 1).ToString());

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            var prevInput = prevTick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            foreach (var kvp in prevInput.players)
            {
                if (input.players.TryGetValue(kvp.Key, out var pp))
                    pp.direction = kvp.Value.direction;
            }

            state.SetInput(input, "6");

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(100000), new Random(1853764998));
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