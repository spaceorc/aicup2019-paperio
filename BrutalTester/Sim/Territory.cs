using System.Collections.Generic;
using System.Linq;
using Game.Protocol;

namespace BrutalTester.Sim
{
    public class Territory
    {
        public Territory(V pos)
        {
            Points.Add(pos);
            Points.UnionWith(pos.GetNeighboring(Env.WIDTH));
        }

        public HashSet<V> Points { get; } = new HashSet<V>();

        public HashSet<V> Capture(List<V> lines)
        {
            var captured = new HashSet<V>();

            if (lines.Count > 0)
            {
                if (Points.Contains(lines[lines.Count - 1]))
                {
                    var voids = GetVoidsBetweenLinesAndTerritory(lines);
                    captured.UnionWith(CaptureVoidsBetweenLines(lines));

                    foreach (var line in lines)
                    {
                        if (!Points.Contains(line))
                            captured.Add(line);
                    }

                    foreach (var @void in voids)
                        captured.UnionWith(DoCapture(@void));
                }
            }

            return captured;
        }

        public List<V> RemovePoints(IEnumerable<V> points)
        {
            var removed = new List<V>();
            foreach (var point in points)
            {
                if (Points.Remove(point))
                    removed.Add(point);
            }

            return removed;
        }

        public List<V> Split(List<V> line, Direction direction, Player player)
        {
            var removed = new List<V>();

            var lPoint = line[0];
            if (line.Any(point => Points.Contains(point)))
            {
                foreach (var point in Points.ToList())
                {
                    if (direction == Direction.Up || direction == Direction.Down)
                    {
                        if (player.Pos.X < lPoint.X)
                        {
                            if (point.X >= lPoint.X)
                            {
                                removed.Add(point);
                                Points.Remove(point);
                            }
                        }
                        else
                        {
                            if (point.X <= lPoint.X)
                            {
                                removed.Add(point);
                                Points.Remove(point);
                            }
                        }
                    }
                    else if (direction == Direction.Left || direction == Direction.Right)
                    {
                        if (player.Pos.Y < lPoint.Y)
                        {
                            if (point.Y >= lPoint.Y)
                            {
                                removed.Add(point);
                                Points.Remove(point);
                            }
                        }
                        else
                        {
                            if (point.Y <= lPoint.Y)
                            {
                                removed.Add(point);
                                Points.Remove(point);
                            }
                        }
                    }
                }
            }

            return removed;
        }

        private List<V> GetBoundary()
        {
            var boundary = new List<V>();
            foreach (var point in Points)
            {
                if (point.GetNeighboring(Env.WIDTH).Any(neighboring => !Points.Contains(neighboring)))
                    boundary.Add(point);
            }

            return boundary;
        }

        private List<V> CaptureVoidsBetweenLines(List<V> lines)
        {
            var captured = new List<V>();

            for (var i = 0; i < lines.Count; i++)
            {
                foreach (var point in lines[i].GetNeighboring(Env.WIDTH))
                {
                    var endIndex = lines.IndexOf(point);
                    if (endIndex >= 0)
                    {
                        // + 1 добавлено тут https://github.com/MailRuChamps/miniaicups/pull/272
                        if (endIndex - i + 1 >= 8)
                        {
                            var path = lines.GetRange(i, endIndex - i + 1); // + 1 добавлено тут https://github.com/MailRuChamps/miniaicups/pull/272
                            captured.AddRange(DoCapture(path));
                        }
                    }
                }
            }

            return captured;
        }

        private List<V> DoCapture(List<V> boundary)
        {
            var poligon_x_arr = boundary.Select(b => b.X).ToArray();
            var poligon_y_arr = boundary.Select(b => b.Y).ToArray();

            var captured = new List<V>();

            var max_x = poligon_x_arr.Max();
            var max_y = poligon_y_arr.Max();
            var min_x = poligon_x_arr.Min();
            var min_y = poligon_y_arr.Min();

            var x = max_x;

            while (x > min_x)
            {
                var y = max_y;
                while (y > min_y)
                {
                    var v = V.Get(x, y);
                    if (!Points.Contains(v) && InPolygon(x, y, poligon_x_arr, poligon_y_arr))
                        captured.Add(v);

                    y -= Env.WIDTH;
                }

                x -= Env.WIDTH;
            }

            return captured;
        }

        private static bool IsSiblings(V p1, V p2)
        {
            return p1.GetVertAndHoriz(Env.WIDTH).Contains(p2);
        }

        private static bool InPolygon(int x, int y, int[] xp, int[] yp)
        {
            var c = false;
            for (var i = 0; i < xp.Length; i++)
            {
                var prev = (i + xp.Length - 1) % xp.Length;
                if ((yp[i] <= y && y < yp[prev] || yp[prev] <= y && y < yp[i]) &&
                    x > (xp[prev] - xp[i]) * (y - yp[i]) / (yp[prev] - yp[i]) + xp[i])
                    c = !c;
            }

            return c;
        }

        private List<List<V>> GetVoidsBetweenLinesAndTerritory(List<V> lines)
        {
            var boundary = GetBoundary();
            var graph = GetGraph(boundary);
            var voids = new List<List<V>>();

            for (var i_lp1 = 0; i_lp1 < lines.Count; i_lp1++)
            {
                var lp1 = lines[i_lp1];
                foreach (var point in lp1.GetNeighboring(Env.WIDTH))
                {
                    if (boundary.Contains(point))
                    {
                        V prev = null;
                        for (var i_lp2 = 0; i_lp2 <= i_lp1; i_lp2++)
                        {
                            var lp2 = lines[i_lp2];
                            var startPoints = GetStartPoints(lp2, boundary);
                            foreach (var startPoint in startPoints)
                            {
                                if (prev != null && (IsSiblings(prev, startPoint) || prev == startPoint))
                                {
                                    prev = startPoint;
                                    continue;
                                }

                                var endIndex = boundary.IndexOf(point);
                                var startIndex = boundary.IndexOf(startPoint);
                                var path = graph.FindPath(endIndex, startIndex);
                                if (path == null)
                                    continue;

                                if (path.Count > 1 && path[0] == path[path.Count - 1])
                                    path = path.GetRange(1, path.Count - 1);

                                voids.Add(
                                    lines
                                        .GetRange(i_lp2, i_lp1 - i_lp2 + 1)
                                        .Concat(path.Select(index => boundary[index]))
                                        .ToList()
                                );
                                prev = startPoint;
                            }
                        }
                    }
                }
            }

            return voids;
        }

        private static List<V> GetStartPoints(V point, List<V> boundary)
        {
            var result = new List<V>();
            foreach (var neighbor in point.GetSelfAndNeighboring(Env.WIDTH))
            {
                if (boundary.Contains(neighbor))
                    result.Add(neighbor);
            }

            return result;
        }

        private static IEnumerable<V> GetSiblings(V point, List<V> boundary)
        {
            return point.GetNeighboring(Env.WIDTH).Where(boundary.Contains);
        }

        private static Graph GetGraph(List<V> boundary)
        {
            var graph = new Graph();
            for (var index = 0; index < boundary.Count; index++)
            {
                var point = boundary[index];
                var siblings = GetSiblings(point, boundary);
                foreach (var sibling in siblings)
                    graph.AddEdge(index, boundary.IndexOf(sibling));
            }

            return graph;
        }
    }
}