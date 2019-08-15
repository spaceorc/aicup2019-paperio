using System.Text;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
{
    public class PathBuilder
    {
        public Direction[] dirs; 
        public int len;
        public int originalLen;

        public void BuildPath(State state, ReliablePathGenerator path, int player)
        {
            if (dirs == null)
                dirs = new Direction[Env.CELLS_COUNT];

            len = path.len;
            originalLen = len;
            dirs[len - 1] = state.players[player].arrivePos.DirTo(path.coords[0]);
            for (var i = 1; i < len; i++)
                dirs[len - i - 1] = path.coords[i - 1].DirTo(path.coords[i]);
        }

        public void BuildPath(State state, DistanceMapGenerator distanceMap, int player, ushort target)
        {
            if (dirs == null)
                dirs = new Direction[Env.CELLS_COUNT];

            len = 0;
            originalLen = 0;
            if (target == ushort.MaxValue)
                return;
            
            var start = state.players[player].arrivePos;
            for (int cur = target; cur != start; )
            {
                var next = distanceMap.paths[player, cur];
                dirs[len++] = ((ushort)next).DirTo((ushort)cur);
                originalLen++;
                cur = next;
            }
        }

        public string Print()
        {
            var result = new StringBuilder();
            for (var i = originalLen - 1; i >= 0; i--)
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