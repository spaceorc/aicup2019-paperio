using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

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
                        {
                            Points.Add(line);
                            captured.Add(line);
                        }
                    }

                    foreach (var @void in voids)
                    {
                        captured.UnionWith(DoCapture(@void));
                    }
                }
            }

            return captured;
        }

        private List<V> CaptureVoidsBetweenLines(List<V> lines)
        {
            var captured = new List<V>();

            for (int i = 0; i < lines.Count; i++)
            {
                foreach (var point in lines[i].GetNeighboring(Env.WIDTH))
                {
                    var endIndex = lines.IndexOf(point);
                    if (endIndex >= 0)
                    {
                        if (endIndex - i >= 8)
                        {
                            var path = lines.GetRange(i, endIndex - i);
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
                    var v = new V(x, y);
                    if (!Points.Contains(v) && InPolygon(x, y, poligon_x_arr, poligon_y_arr))
                    {
                        Points.Add(v);
                        captured.Add(v);
                    }

                    y -= Env.WIDTH;
                }

                x -= Env.WIDTH;
            }

            return captured;
        }

        private bool InPolygon(int x, int y, int[] xp, int[] yp)
        {
            var c = false;
            for (int i = 0; i < xp.Length; i++)
            {
                var prev = (i + xp.Length - 1) % xp.Length;
                if (((yp[i] <= y && y < yp[prev]) || (yp[prev] <= y && y < yp[i])) &&
                    (x > (xp[prev] - xp[i]) * (y - yp[i]) / (yp[prev] - yp[i]) + xp[i]))
                    c = !c;
            }

            return c;
        }

        private List<List<V>> GetVoidsBetweenLinesAndTerritory(List<V> lines)
        {
            var boundary = GetBoundary();
            var voids = new List<List<V>>();

            foreach (var cur in lines)
            {
                foreach (var point in cur.GetNeighboring(Env.WIDTH))
                {
                    if (boundary.Contains(point))
                    {
                        var startPoint = GetNearestBoundary(lines[0], boundary);
                        if (startPoint != null)
                        {
                            var endIndex = boundary.IndexOf(point);
                            var startIndex = boundary.IndexOf(startPoint);
                            var path = GetPath(startIndex, endIndex, boundary);
                            if (path == null)
                                continue;

                            if (path.Count > 1 && path[0] == path[path.Count - 1])
                                path = path.GetRange(1, path.Count - 1);

                            voids.Add(
                                lines
                                    .GetRange(0, lines.IndexOf(cur) + 1)
                                    .Concat(path.Select(index => boundary[index]))
                                    .ToList()
                            );
                        }
                    }
                }
            }

            return voids;
        }

        private V GetNearestBoundary(V point, List<V> boundary)
        {
            foreach (var neighbor in point.GetSelfAndNeighboring(Env.WIDTH))
            {
                if (boundary.Contains(neighbor))
                    return neighbor;
            }

            return null;
        }

        public List<V> GetBoundary()
        {
            var boundary = new List<V>();
            foreach (var point in Points)
            {
                if (point.GetNeighboring(Env.WIDTH).Any(neighboring => !Points.Contains(neighboring)))
                    boundary.Add(point);
            }

            return boundary;
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

        private List<int> GetPath(int startIndex, int endIndex, List<V> boundary)
        {
            var graph = new Graph();
            for (int index = 0; index < boundary.Count; index++)
            {
                var point = boundary[index];
                var siblings = point.GetVertAndHoriz(Env.WIDTH).Where(boundary.Contains);
                foreach (var sibling in siblings)
                    graph.AddEdge(index, boundary.IndexOf(sibling));
            }

            return graph.FindPath(endIndex, startIndex);
        }
    }
}