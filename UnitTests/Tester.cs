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
                @"{""players"":{""2"":{""score"":482,""direction"":""left"",""territory"":[[225,675],[345,735],[645,615],[225,645],[345,705],[285,525],[495,585],[405,525],[195,645],[315,765],[555,615],[645,645],[645,555],[345,615],[615,645],[525,585],[405,465],[135,765],[645,585],[165,645],[315,555],[315,645],[195,765],[375,525],[465,615],[675,525],[375,555],[195,705],[615,585],[465,645],[165,555],[645,525],[225,555],[255,735],[135,585],[255,615],[225,525],[195,555],[135,615],[285,705],[345,525],[675,585],[315,585],[315,675],[255,705],[375,645],[435,585],[555,645],[675,555],[255,675],[405,615],[225,705],[165,585],[285,615],[165,735],[525,645],[315,615],[405,555],[135,555],[195,615],[495,645],[225,735],[255,765],[135,705],[525,615],[135,735],[285,675],[675,615],[165,675],[315,705],[255,585],[555,585],[195,735],[225,615],[255,555],[405,495],[195,675],[285,585],[345,645],[135,645],[585,615],[135,675],[345,555],[585,585],[225,585],[165,705],[345,585],[285,645],[285,765],[195,525],[255,645],[435,645],[375,585],[495,615],[375,615],[345,765],[165,525],[285,555],[405,645],[315,735],[585,645],[165,765],[615,615],[405,585],[195,585],[225,765],[465,585],[435,615],[285,735],[165,615],[315,525],[255,525],[345,675]],""position"":[225,825],""lines"":[[705,555],[735,555],[735,585],[735,615],[705,615],[705,645],[705,675],[705,705],[675,705],[645,705],[615,705],[585,705],[555,705],[525,705],[495,705],[465,705],[465,735],[465,765],[465,795],[465,825],[435,825],[405,825],[375,825],[345,825],[315,825],[285,825],[255,825],[225,825]],""bonuses"":[]},""3"":{""score"":601,""direction"":""right"",""territory"":[[705,195],[555,405],[645,465],[465,345],[615,465],[495,435],[555,435],[615,195],[465,255],[675,405],[675,315],[495,285],[585,465],[645,225],[645,375],[525,225],[525,375],[585,315],[465,285],[615,315],[675,375],[495,225],[585,285],[555,375],[645,405],[555,345],[645,315],[375,345],[405,435],[435,405],[465,435],[615,405],[495,375],[525,345],[375,375],[645,345],[495,315],[675,195],[405,405],[615,225],[615,285],[435,435],[555,285],[525,315],[585,375],[435,345],[675,435],[675,285],[615,255],[465,375],[705,225],[645,195],[465,315],[405,345],[585,345],[645,255],[585,225],[405,375],[465,225],[615,345],[615,435],[465,405],[525,285],[555,315],[435,375],[645,285],[555,465],[585,195],[495,405],[495,255],[615,375],[525,255],[585,435],[675,345],[495,345],[555,255],[585,405],[675,225],[435,165],[645,435],[555,225],[585,255]],""position"":[705,345],""lines"":[[705,345]],""bonuses"":[]},""4"":{""score"":534,""direction"":""left"",""territory"":[[705,315],[585,555],[615,555],[465,525],[525,405],[435,555],[585,525],[435,465],[465,495],[435,495],[705,285],[525,555],[555,525],[435,525],[495,525],[615,495],[645,495],[675,465],[465,465],[495,465],[675,495],[525,525],[555,555],[525,435],[525,495],[675,255],[495,555],[555,495],[705,255],[585,495],[465,555],[615,525],[525,465],[495,495]],""position"":[345,405],""lines"":[[465,435],[465,405],[435,405],[405,405],[375,405],[345,405]],""bonuses"":[]},""i"":{""score"":406,""direction"":""down"",""territory"":[[375,255],[225,375],[255,435],[165,345],[405,255],[75,405],[285,345],[345,405],[135,285],[75,135],[105,315],[45,375],[135,315],[195,375],[15,225],[405,195],[195,105],[345,315],[105,165],[45,405],[255,255],[165,195],[135,195],[225,345],[285,255],[285,405],[345,345],[165,435],[255,345],[375,225],[315,195],[255,225],[345,255],[75,195],[45,315],[105,375],[255,315],[195,435],[345,225],[15,165],[225,225],[75,435],[195,165],[285,315],[165,105],[165,255],[135,405],[105,225],[75,165],[45,345],[135,135],[135,435],[15,105],[225,255],[105,135],[75,285],[195,285],[255,405],[45,255],[435,225],[315,285],[315,225],[435,285],[195,225],[345,435],[75,225],[225,435],[45,285],[105,255],[225,135],[345,195],[165,285],[225,405],[285,285],[105,195],[15,405],[105,105],[405,285],[15,435],[75,345],[135,105],[345,375],[195,345],[15,345],[15,195],[105,405],[165,135],[405,225],[285,225],[75,315],[225,105],[165,225],[165,375],[45,225],[435,255],[375,165],[255,285],[315,315],[315,405],[255,165],[375,285],[75,375],[375,195],[375,315],[15,375],[195,405],[15,285],[15,135],[405,165],[195,135],[315,255],[15,315],[135,225],[225,315],[75,105],[45,165],[105,285],[165,405],[135,255],[285,195],[225,285],[345,285],[45,195],[315,345],[315,435],[195,255],[375,405],[435,195],[375,435],[225,195],[195,195],[345,165],[15,255],[225,165],[135,165],[285,375],[165,315],[45,105],[105,435],[105,345],[315,375],[405,315],[75,255],[135,345],[45,135],[255,375],[45,435],[195,315],[135,375],[285,165],[165,165],[285,435],[435,315],[315,165],[255,195]],""position"":[45,525],""lines"":[[315,465],[315,495],[315,525],[315,555],[285,555],[255,555],[255,525],[225,525],[195,525],[165,525],[165,555],[135,555],[105,555],[75,555],[45,555],[45,525]],""bonuses"":[]}},""bonuses"":[{""type"":""n"",""position"":[45,495],""active_ticks"":50},{""type"":""saw"",""position"":[105,675],""active_ticks"":40}],""tick_num"":913}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000, 100000), new Random(-1581423847));

            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Debug_visio()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (27)"));

            var visioInfo = (JArray)visio["visio_info"];
            var config = visioInfo.Single(x => x["type"]?.ToString() == "start_game").ToObject<Config>();
            ;
            config.ApplyToEnv();

            var t = 883;

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

            // var finder = new AllowedDirectionsFinder(1);
            //
            // var distanceMap = new DistanceMap();
            // distanceMap.Build(state);
            //
            // var facts = new InterestingFacts();
            // facts.Build(state, distanceMap);
            //
            // var sw = Stopwatch.StartNew();
            // var mask = finder.GetAllowedDirectionsMask(new TestingTimeManager(100000, 1000), state, 0, distanceMap, facts);
            // sw.Stop();
            //
            // Console.Out.WriteLine(
            //     $"Elapsed: {sw.ElapsedMilliseconds}. " +
            //     $"{AllowedDirectionsFinder.DescribeAllowedDirectionsMask(mask)}. " +
            //     $"Estimations: {finder.minimax.estimations}. " +
            //     $"BestScore: {finder.minimax.bestScore}. " +
            //     $"BestDepth: {finder.minimax.bestDepth}. " +
            //     $"Results: {string.Join(",", finder.minimax.bestResultScores)}.");

            var ai = new RandomWalkAi();
            
            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000, 1000), new Random(1853764998));
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
            var commands = new[] {Direction.Down};
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
            private readonly int nestedCounter;

            public TestingTimeManager(int counter, int nestedCounter)
            {
                this.counter = counter;
                this.nestedCounter = nestedCounter;
            }

            public bool IsExpired => counter-- <= 0;

            public ITimeManager GetNested(int millis) => new TestingTimeManager(nestedCounter, nestedCounter);
        }
    }
}