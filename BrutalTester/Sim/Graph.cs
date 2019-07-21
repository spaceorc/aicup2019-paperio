using System.Collections.Generic;

namespace BrutalTester.Sim
{
    public class Graph
    {
        private readonly Dictionary<int, HashSet<int>> edges = new Dictionary<int, HashSet<int>>();

        public void AddEdge(int a, int b)
        {
            edges.TryAdd(a, new HashSet<int>());
            edges.TryAdd(b, new HashSet<int>());
            edges[a].Add(b);
            edges[b].Add(a);
        }

        public List<int> FindPath(int start, int end)
        {
            var queue = new Queue<int>();
            queue.Enqueue(start);
            var used = new Dictionary<int, int>
            {
                {start, start}
            };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == end)
                {
                    var result = new List<int>();
                    for (; cur != start; cur = used[cur])
                        result.Add(cur);
                    result.Add(start);
                    result.Reverse();
                    return result;
                }

                if (edges.TryGetValue(cur, out var curEdges))
                {
                    foreach (var next in curEdges)
                    {
                        if (!used.ContainsKey(next))
                        {
                            queue.Enqueue(next);
                            used.Add(next, cur);
                        }
                    }
                }
            }

            return null;
        }
    }
}