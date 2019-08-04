using System.Text;
using Game.Fast;
using Game.Types;

namespace Game.Strategies
{
    public class PathBuilder
    {
        public Direction[] dirs; 
        public int len;
        public int originalLen;

        public void BuildPath(FastState state, RandomPathGenerator path, int player)
        {
            if (dirs == null)
                dirs = new Direction[state.config.x_cells_count * state.config.y_cells_count];

            len = path.len;
            originalLen = len;
            dirs[len - 1] = state.MakeDir(state.players[player].arrivePos, path.coords[0]);
            for (int i = 1; i < len; i++)
                dirs[len - i - 1] = state.MakeDir(path.coords[i - 1], path.coords[i]);
        }

        public void BuildPath(FastState state, DistanceMapGenerator distanceMap, int player, ushort target)
        {
            if (dirs == null)
                dirs = new Direction[state.config.x_cells_count * state.config.y_cells_count];

            len = 0;
            originalLen = 0;
            if (target == ushort.MaxValue)
                return;
            
            var start = state.players[player].arrivePos;
            for (int cur = target; cur != start; )
            {
                var next = distanceMap.paths[player, cur];
                dirs[len++] = state.MakeDir((ushort)next, (ushort)cur);
                originalLen++;
                cur = next;
            }
        }

        public string Print()
        {
            var result = new StringBuilder();
            for (int i = originalLen - 1; i >= 0; i--)
                result.Append(dirs[i].ToString()[0]);
            return result.ToString();
        }

        public void Clear()
        {
            len = 0;
            originalLen = 0;
        }
    }
}