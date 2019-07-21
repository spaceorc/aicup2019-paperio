using System;
using BrutalTester.Sim;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Graph_Test
    {
        private Graph graph;

        [SetUp]
        public void SetUp()
        {
            graph = new Graph();
        }

        [TestCase("", 1, 2, null)]
        [TestCase("1,2", 1, 2, "1,2")]
        [TestCase("1,2", 1, 3, null)]
        [TestCase("1,2", 3, 1, null)]
        [TestCase("1,2; 2,3; 3,4; 2,4", 1, 4, "1,2,4")]
        [TestCase("1,2; 2,3; 3,4; 2,4", 4, 1, "4,2,1")]
        public void Test(string edges, int start, int end, string expected)
        {
            foreach (var edge in edges.Split(new[] {';', ' '}, StringSplitOptions.RemoveEmptyEntries))
                graph.AddEdge(int.Parse(edge.Split(',')[0]), int.Parse(edge.Split(',')[1]));

            var path = graph.FindPath(start, end);
            if (expected == null)
                path.Should().BeNull();
            else
                string.Join(",", path).Should().Be(expected);
        }
    }
}