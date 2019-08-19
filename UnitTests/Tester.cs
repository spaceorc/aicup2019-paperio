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
                @"{""players"":{""2"":{""score"":829,""direction"":""right"",""territory"":[[75,615],[225,675],[465,915],[345,735],[225,375],[255,435],[405,855],[375,855],[225,645],[105,765],[285,525],[195,915],[165,795],[345,705],[345,405],[195,645],[315,765],[315,855],[285,795],[105,705],[405,795],[75,735],[195,375],[495,735],[255,855],[465,825],[345,885],[135,765],[525,735],[105,525],[165,645],[135,495],[285,405],[315,555],[555,795],[195,765],[315,645],[435,855],[195,495],[75,465],[225,855],[255,915],[195,705],[345,915],[405,735],[195,435],[105,735],[165,555],[315,885],[315,495],[105,675],[105,585],[75,765],[495,765],[75,495],[225,555],[255,885],[255,735],[465,795],[345,855],[135,585],[195,825],[525,855],[255,615],[225,825],[225,525],[195,555],[135,615],[345,525],[285,705],[435,825],[315,585],[255,405],[315,675],[255,705],[435,735],[495,855],[375,765],[345,435],[255,795],[465,885],[225,435],[255,675],[375,795],[225,705],[165,585],[495,795],[285,615],[225,405],[405,885],[345,465],[165,735],[75,645],[135,525],[285,885],[195,885],[525,795],[315,615],[105,555],[195,615],[135,555],[225,735],[345,375],[405,825],[255,765],[135,705],[255,495],[135,735],[525,765],[285,675],[165,675],[315,705],[435,915],[255,585],[315,405],[495,885],[195,735],[225,915],[375,885],[75,705],[225,615],[255,825],[465,855],[105,615],[255,555],[375,915],[225,885],[195,675],[495,825],[285,585],[405,765],[75,675],[195,405],[135,645],[285,855],[165,495],[525,915],[135,675],[465,765],[345,825],[225,585],[165,705],[495,915],[285,495],[195,795],[285,645],[435,885],[525,885],[285,465],[75,525],[315,795],[285,765],[195,525],[285,915],[255,645],[435,795],[315,435],[345,495],[195,465],[345,765],[375,735],[105,495],[225,465],[165,525],[285,555],[555,765],[285,375],[315,735],[285,825],[165,765],[405,915],[315,375],[195,855],[225,795],[525,825],[135,795],[75,585],[225,495],[465,735],[195,585],[345,795],[255,375],[105,645],[225,765],[75,555],[435,765],[285,435],[165,615],[285,735],[315,915],[315,825],[315,525],[255,465],[255,525],[315,465],[375,705],[375,825]],""position"":[135,435],""lines"":[[105,435],[135,435]],""bonuses"":[]},""3"":{""score"":467,""direction"":""up"",""territory"":[[885,885],[915,765],[885,795],[795,735],[735,705],[855,825],[855,915],[735,765],[585,765],[915,855],[705,705],[795,765],[825,915],[855,885],[825,885],[615,765],[705,735],[795,795],[765,795],[885,855],[915,795],[885,765],[675,765],[555,825],[585,825],[825,855],[795,705],[855,735],[795,825],[825,825],[765,705],[885,915],[885,825],[585,795],[765,765],[705,795],[735,795],[825,795],[855,795],[825,765],[705,765],[885,705],[765,735],[915,825],[885,735],[855,705],[645,765],[825,735],[855,765],[855,855],[825,705],[735,735]],""position"":[465,855],""lines"":[[915,735],[915,705],[915,675],[915,645],[915,615],[915,585],[915,555],[915,525],[885,525],[855,525],[825,525],[795,525],[765,525],[735,525],[705,525],[705,555],[705,585],[705,615],[705,645],[675,645],[675,675],[675,705],[645,705],[615,705],[585,705],[585,735],[555,735],[525,735],[525,765],[525,795],[525,825],[495,825],[465,825],[465,855]],""bonuses"":[]},""4"":{""score"":444,""direction"":""down"",""territory"":[[165,135],[195,75],[285,75],[285,225],[225,105],[255,285],[255,165],[195,105],[225,75],[255,255],[285,135],[225,45],[285,255],[165,45],[195,135],[255,225],[255,75],[285,45],[285,195],[225,285],[225,225],[165,105],[135,135],[255,45],[255,195],[225,255],[225,195],[165,75],[285,105],[225,165],[255,135],[255,105],[225,135],[135,75],[285,165],[285,285],[195,45],[315,75],[135,105]],""position"":[285,315],""lines"":[[165,165],[135,165],[105,165],[75,165],[45,165],[15,165],[15,135],[15,105],[15,75],[15,45],[45,45],[45,15],[75,15],[105,15],[135,15],[165,15],[195,15],[225,15],[255,15],[285,15],[315,15],[345,15],[375,15],[405,15],[435,15],[435,45],[435,75],[405,75],[405,105],[405,135],[405,165],[405,195],[405,225],[405,255],[405,285],[375,285],[375,315],[375,345],[375,375],[345,375],[315,375],[285,375],[285,345],[285,315]],""bonuses"":[]},""5"":{""score"":647,""direction"":""right"",""territory"":[[555,135],[735,285],[705,195],[615,15],[495,45],[675,45],[375,255],[795,15],[855,285],[915,315],[735,15],[855,105],[345,135],[735,315],[915,15],[465,195],[405,255],[615,105],[525,45],[465,255],[405,195],[885,255],[585,75],[705,105],[495,135],[345,315],[645,225],[915,255],[525,225],[855,165],[345,75],[465,105],[705,15],[495,225],[825,315],[675,105],[795,75],[765,195],[825,15],[735,195],[705,285],[885,135],[435,45],[885,45],[375,225],[315,105],[315,195],[825,285],[555,75],[885,315],[345,255],[765,255],[615,45],[795,135],[855,15],[345,225],[915,45],[795,255],[405,135],[585,135],[645,165],[525,165],[405,75],[705,75],[495,165],[645,195],[645,105],[735,135],[705,345],[915,135],[465,75],[675,255],[825,195],[765,15],[795,195],[435,225],[555,45],[375,45],[585,45],[315,285],[885,15],[315,225],[765,285],[795,315],[435,285],[825,165],[765,105],[435,135],[375,75],[885,285],[585,195],[555,195],[855,45],[345,195],[915,75],[465,165],[405,15],[735,105],[555,255],[645,135],[645,45],[405,285],[525,75],[645,75],[405,225],[705,315],[585,105],[915,165],[855,195],[345,45],[615,195],[435,255],[825,75],[765,135],[855,255],[435,105],[555,165],[375,165],[885,225],[315,15],[315,315],[375,285],[825,345],[795,45],[765,225],[825,45],[735,45],[435,15],[375,195],[495,285],[675,135],[375,315],[855,345],[915,105],[465,135],[525,15],[705,165],[495,75],[645,15],[405,165],[855,315],[585,165],[315,255],[735,255],[765,165],[525,195],[615,135],[825,105],[405,105],[315,165],[915,195],[855,225],[345,15],[465,45],[795,105],[735,225],[345,285],[675,195],[825,255],[615,225],[555,105],[885,195],[315,45],[885,105],[315,135],[375,135],[555,285],[825,225],[765,45],[795,165],[675,285],[555,15],[435,195],[375,15],[705,225],[495,15],[795,285],[765,315],[675,165],[675,15],[435,75],[585,225],[915,285],[855,75],[345,165],[465,225],[525,135],[525,285],[705,135],[735,345],[615,75],[495,105],[405,45],[705,255],[735,75],[405,315],[735,165],[495,255],[615,165],[525,105],[915,225],[525,255],[855,135],[345,105],[465,15],[795,225],[675,345],[585,15],[705,45],[495,195],[795,345],[675,225],[675,75],[435,315],[765,75],[825,135],[435,165],[375,105],[555,225],[885,165],[885,75],[765,345],[675,315]],""position"":[615,465],""lines"":[[585,255],[585,285],[585,315],[585,345],[585,375],[585,405],[585,435],[585,465],[615,465]],""bonuses"":[]},""6"":{""score"":594,""direction"":""down"",""territory"":[[765,615],[585,705],[795,405],[765,435],[915,615],[645,615],[705,465],[795,615],[495,585],[615,555],[555,615],[645,645],[645,555],[735,495],[345,615],[615,645],[525,585],[705,555],[915,705],[645,585],[495,675],[675,705],[825,615],[735,465],[795,465],[855,495],[705,615],[585,735],[435,705],[795,675],[765,645],[825,585],[765,465],[885,405],[675,675],[675,525],[465,615],[375,555],[855,585],[645,495],[705,435],[615,585],[465,645],[735,585],[855,555],[555,735],[645,525],[405,675],[735,405],[915,735],[525,705],[855,645],[915,435],[855,465],[735,435],[495,705],[675,585],[435,675],[825,495],[885,435],[855,435],[435,585],[375,645],[825,465],[735,615],[555,645],[675,555],[795,525],[765,555],[375,675],[735,525],[915,525],[405,615],[735,555],[525,645],[645,435],[705,495],[705,645],[405,555],[885,615],[495,645],[915,915],[645,465],[915,465],[675,735],[525,615],[675,615],[825,675],[795,585],[435,555],[825,645],[765,585],[555,585],[765,405],[795,435],[885,675],[675,405],[885,585],[465,675],[795,645],[765,675],[855,525],[915,555],[645,735],[345,645],[465,705],[645,405],[525,555],[585,615],[885,495],[345,555],[405,705],[735,645],[585,585],[915,645],[915,495],[855,405],[735,675],[675,465],[345,585],[675,495],[615,675],[825,555],[555,555],[705,525],[435,645],[375,585],[825,525],[555,705],[885,645],[675,435],[885,555],[495,615],[885,465],[765,495],[375,615],[915,885],[915,585],[705,675],[795,495],[585,675],[495,555],[405,645],[585,645],[855,615],[645,675],[705,405],[525,675],[465,555],[615,615],[405,585],[915,675],[705,585],[645,705],[465,585],[615,705],[435,615],[825,435],[555,675],[855,675],[795,555],[765,525],[615,735],[825,405],[345,675],[885,525],[675,645]],""position"":[255,655],""lines"":[[375,705],[345,705],[315,705],[285,705],[255,705],[255,675]],""bonuses"":[]},""i"":{""score"":589,""direction"":""down"",""territory"":[[45,45],[45,795],[165,345],[75,405],[285,345],[135,285],[15,645],[45,75],[75,135],[105,315],[45,375],[135,15],[135,315],[15,675],[15,225],[135,465],[15,585],[105,165],[45,405],[165,195],[135,195],[225,345],[345,345],[165,435],[255,345],[45,735],[375,345],[45,15],[75,195],[375,375],[105,375],[45,315],[255,315],[165,15],[15,765],[15,615],[15,165],[75,435],[195,165],[285,315],[165,255],[105,225],[15,525],[75,165],[135,405],[45,345],[135,435],[15,555],[15,105],[105,135],[15,465],[105,45],[45,675],[165,465],[75,285],[195,285],[75,15],[45,255],[45,705],[195,225],[75,225],[45,285],[105,255],[165,285],[15,495],[15,45],[105,195],[15,405],[105,105],[45,615],[15,435],[75,345],[15,795],[195,345],[75,75],[105,15],[15,345],[15,195],[105,405],[45,645],[75,315],[165,225],[165,375],[75,45],[45,225],[75,375],[45,555],[15,375],[135,45],[15,735],[105,75],[15,285],[15,135],[105,465],[45,585],[15,315],[135,225],[225,315],[75,105],[45,165],[105,285],[15,75],[135,255],[165,405],[75,795],[45,195],[315,345],[45,495],[195,255],[375,405],[375,435],[195,195],[45,525],[15,255],[135,165],[165,315],[45,105],[105,435],[105,345],[15,15],[105,795],[75,255],[135,345],[15,705],[45,135],[45,435],[195,315],[135,375],[165,165],[45,465],[45,765]],""position"":[165,285],""lines"":[],""bonuses"":[]}},""bonuses"":[{""type"":""n"",""position"":[615,885],""active_ticks"":40}],""tick_num"":1003}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(input);

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi();

            var command = ai.GetCommand(state, 0, new TestingTimeManager(10000, 1000), new Random(2064031676));

            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Debug_visio()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (32)"));

            var visioInfo = (JArray)visio["visio_info"];
            var config = visioInfo.Single(x => x["type"]?.ToString() == "start_game").ToObject<Config>();
            ;
            config.ApplyToEnv();

            var t = 1833;

            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == t.ToString());
            var prevTick = visioInfo.Single(x => x["tick_num"]?.ToString() == (t - 1).ToString());

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            var prevInput = prevTick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            foreach (var kvp in prevInput.players)
            {
                if (input.players.TryGetValue(kvp.Key, out var pp))
                    pp.direction = kvp.Value.direction;
            }

            state.SetInput(input, "2");

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
            //var ai = new RandomWalkAi(new NearestOpponentStartPathStrategy(), new CaptureOpponentEstimator(), useAllowedDirections: true, useTerritoryTtl: true);

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