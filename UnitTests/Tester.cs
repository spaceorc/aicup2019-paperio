using System;
using System.IO;
using System.Linq;
using Game.BruteForce;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;
using Game.Strategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public unsafe class Tester
    {
        [Test]
        public void Debug()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var input = JsonConvert.DeserializeObject<RequestInput>(
                @"{""players"":{""1"":{""score"":1143,""direction"":""right"",""territory"":[[765,615],[225,375],[255,435],[645,615],[705,465],[405,255],[495,585],[345,405],[285,345],[615,555],[405,525],[525,405],[555,615],[585,255],[735,765],[645,645],[645,555],[195,375],[735,495],[465,255],[345,315],[615,645],[405,465],[525,585],[705,555],[645,585],[585,315],[525,375],[225,345],[675,375],[465,285],[675,705],[285,405],[345,345],[735,465],[165,435],[585,285],[435,495],[255,345],[705,615],[375,525],[555,345],[765,645],[375,345],[195,495],[435,405],[435,525],[495,525],[765,465],[495,375],[615,495],[555,525],[465,435],[675,525],[675,675],[375,555],[375,375],[255,315],[645,495],[195,435],[465,465],[285,315],[615,585],[495,315],[405,405],[525,525],[735,585],[645,525],[525,315],[585,375],[585,345],[405,345],[165,465],[525,495],[345,525],[615,435],[675,585],[555,315],[195,285],[435,375],[255,405],[315,285],[555,465],[435,585],[735,615],[585,495],[435,285],[555,645],[375,465],[615,525],[675,555],[765,735],[765,555],[345,435],[225,435],[735,525],[585,435],[225,405],[735,555],[345,465],[285,285],[495,345],[585,405],[555,255],[405,285],[525,645],[645,435],[705,495],[705,645],[525,435],[405,555],[465,315],[495,645],[735,735],[615,375],[345,375],[495,495],[465,495],[195,345],[645,465],[585,555],[525,615],[465,345],[615,465],[675,615],[495,435],[465,525],[555,435],[435,555],[735,705],[585,525],[435,255],[255,285],[315,315],[315,405],[765,585],[435,465],[555,585],[675,405],[495,285],[375,495],[585,465],[765,675],[645,375],[705,705],[405,495],[615,315],[195,405],[555,375],[165,495],[645,405],[645,315],[405,435],[585,615],[525,555],[705,735],[345,555],[615,405],[525,345],[225,315],[735,645],[585,585],[645,345],[165,405],[225,285],[735,675],[675,465],[345,585],[495,465],[615,675],[345,285],[675,495],[285,465],[435,435],[555,555],[705,525],[315,345],[315,435],[555,285],[375,585],[765,705],[375,405],[435,345],[675,435],[615,255],[465,375],[345,495],[495,615],[195,465],[765,495],[375,435],[645,255],[705,675],[765,765],[405,375],[225,465],[615,345],[465,405],[495,555],[285,375],[525,285],[585,645],[555,495],[645,285],[645,675],[315,375],[405,315],[495,405],[465,555],[495,255],[615,615],[525,465],[405,585],[255,375],[705,585],[525,255],[195,315],[675,345],[465,585],[285,435],[435,315],[255,465],[315,465],[765,525],[555,405],[615,285],[675,645]],""position"":[215,465],""lines"":[],""bonuses"":[]},""i"":{""score"":696,""direction"":""left"",""territory"":[[225,675],[585,705],[345,735],[615,825],[225,645],[285,525],[345,705],[195,645],[405,795],[495,735],[345,615],[465,825],[525,735],[165,645],[705,825],[495,675],[315,555],[555,795],[585,735],[315,645],[615,765],[435,705],[675,795],[465,615],[195,705],[405,735],[465,645],[165,555],[315,495],[555,735],[405,675],[645,825],[495,765],[465,795],[225,555],[255,735],[525,705],[585,795],[255,615],[225,525],[705,795],[195,555],[495,705],[285,705],[375,705],[435,675],[315,585],[315,675],[255,705],[435,735],[375,645],[675,825],[375,765],[375,675],[375,795],[255,675],[405,615],[645,765],[225,705],[165,585],[495,795],[285,615],[525,795],[315,615],[645,795],[195,615],[705,765],[225,735],[255,495],[675,735],[525,765],[285,675],[165,675],[315,705],[255,585],[585,765],[465,675],[225,615],[255,555],[645,735],[195,675],[495,825],[285,585],[345,645],[465,705],[405,765],[465,765],[405,705],[225,585],[675,765],[285,495],[555,825],[285,645],[585,825],[195,525],[255,645],[435,795],[435,645],[555,705],[375,615],[345,765],[375,735],[615,795],[585,675],[165,525],[285,555],[555,765],[405,645],[315,735],[525,675],[525,825],[465,735],[225,495],[195,585],[645,705],[615,705],[435,615],[435,765],[165,615],[285,735],[555,675],[315,525],[255,525],[615,735],[345,675]],""position"":[255,465],""lines"":[[675,705],[705,705],[735,705],[735,675],[765,675],[765,645],[735,645],[705,645],[675,645],[675,615],[645,615],[615,615],[585,615],[585,585],[555,585],[555,555],[525,555],[495,555],[465,555],[435,555],[435,525],[405,525],[375,525],[375,495],[375,465],[375,435],[345,435],[345,405],[315,405],[285,405],[285,435],[285,465],[255,465]],""bonuses"":[]}},""bonuses"":[{""type"":""s"",""position"":[15,315]}],""tick_num"":1289}",
                ConsoleProtocol.jsonSerializerSettings);

            state.SetInput(input);

            Console.Out.WriteLine(state.Print());

            var ai = new MinimaxAi(2);

            var command = ai.GetCommand(state, 0, new TestingTimeManager(1500000), new Random(1848632933));

            Console.Out.WriteLine(command.ToJson());
        }

        [Test]
        public void Debug_visio()
        {
            Logger.enableFile = true;
            Logger.minLevel = Logger.Level.Debug;

            var state = new State();

            var visio = JObject.Parse(File.ReadAllText("/Users/spaceorc/Downloads/visio (21)"));

            var visioInfo = (JArray)visio["visio_info"];
            var t = 1129;

            var tick = visioInfo.Single(x => x["tick_num"]?.ToString() == t.ToString());
            var prevTick = visioInfo.Single(x => x["tick_num"]?.ToString() == (t - 1).ToString());

            var input = tick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            var prevInput = prevTick.ToObject<RequestInput>(JsonSerializer.Create(ConsoleProtocol.jsonSerializerSettings));
            foreach (var kvp in prevInput.players)
            {
                if (input.players.TryGetValue(kvp.Key, out var pp))
                    pp.direction = kvp.Value.direction;
            }

            state.SetInput(input, "3");

            Console.Out.WriteLine(state.Print());

            var ai = new RandomWalkAi(new NearestOpponentStartPathStrategy(), new CaptureOpponentEstimator(), walkOnTerritory: true);

            var command = ai.GetCommand(state, 0, new TestingTimeManager(100000), new Random(773189632));
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
    }
}