using System.Collections.Generic;

namespace BrutalTester.Sim
{
    public class Graph
    {
        private readonly Dictionary<int, HashSet<int>> edges = new Dictionary<int, HashSet<int>>();

        public void AddEdge(int a, int b)
        {
            if (!edges.TryGetValue(a, out var aset))
                edges.Add(a, aset = new HashSet<int>());
            if (!edges.TryGetValue(b, out var bset))
                edges.Add(b, bset = new HashSet<int>());
            aset.Add(b);
            bset.Add(a);
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