using System;
using System.Collections.Generic;
using System.Linq;
using BrutalTester.Sim;
using FluentAssertions;
using Game.Protocol;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Territory_Test
    {
        [TestCase("right", "down", "left", "up")]
        [TestCase("down", "left", "up", "right")]
        [TestCase("left", "up", "right", "down")]
        [TestCase("up", "right", "down", "left")]
        [TestCase("down", "right", "up", "left")]
        [TestCase("right", "up", "left", "down")]
        [TestCase("up", "left", "down", "right")]
        [TestCase("left", "down", "right", "up")]
        public void Capture(string d1, string d2, string d3, string d4)
        {
            //Env.WIDTH = 1;
            var territory = new Territory(V.Get(0, 0));

            var shifts = new Dictionary<string, V>
            {
                {"right", V.Get(1, 0)},
                {"left", V.Get(-1, 0)},
                {"up", V.Get(0, 1)},
                {"down", V.Get(0, -1)},
            };

            var s1 = shifts[d1];
            var s2 = shifts[d2];
            var s3 = shifts[d3];
            var s4 = shifts[d4];

            var p = new List<V>
            {
                V.Get(10, 10)
            };
            p.Add(p.Last() + s1);
            p.Add(p.Last() + s1);
            p.Add(p.Last() + s2);
            p.Add(p.Last() + s1);
            p.Add(p.Last() + s1);
            p.Add(p.Last() + s2);
            p.Add(p.Last() + s2);
            p.Add(p.Last() + s3);
            p.Add(p.Last() + s3);
            p.Add(p.Last() + s3);
            p.Add(p.Last() + s4);
            p.Add(p.Last() + s3);

            territory.Points.Clear();
            territory.Points.Add(p.First() * Env.WIDTH);
            territory.Points.Add(p.Last() * Env.WIDTH);

            var capture = territory.Capture(p.Skip(1).Select(x => x * Env.WIDTH).ToList());

            foreach (var v in capture)
                Console.Out.WriteLine($"{v / Env.WIDTH}->{p.IndexOf(v / Env.WIDTH) + 1:X}");

            capture.Select(x => x / Env.WIDTH)
                .Should()
                .BeEquivalentTo(
                    p.Except(new[]{p.First(), p.Last()})
                        .Concat(
                        new[]
                        {
                            p[0] + s1 * 2 + s2 * 2,
                            p[0] + s1 * 3 + s2 * 2
                        }));
        }
    }
}