using System.Runtime.CompilerServices;
using System.Text;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk
{
    public class PlayerPath
    {
        public int len;
        
        private readonly Direction[] dirs = new Direction[Env.CELLS_COUNT];
        private readonly ushort[] coords = new ushort[Env.CELLS_COUNT];
        private int originalLen;

        public void BuildPath(State state, ReliablePathBuilder path, int player)
        {
            len = path.len;
            originalLen = len;
            dirs[len - 1] = state.players[player].arrivePos.DirTo(path.coords[0]);
            coords[len - 1] = path.coords[0];
            for (var i = 1; i < len; i++)
            {
                dirs[len - i - 1] = path.coords[i - 1].DirTo(path.coords[i]);
                coords[len - i - 1] = path.coords[i];
            }
        }

        public void BuildPath(State state, DistanceMap distanceMap, int player, ushort target)
        {
            len = 0;
            originalLen = 0;
            if (target == ushort.MaxValue)
                return;

            var start = state.players[player].arrivePos;
            for (int cur = target; cur != start;)
            {
                var next = distanceMap.paths[player, cur];
                dirs[len++] = ((ushort)next).DirTo((ushort)cur);
                originalLen++;
                cur = next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Direction CurrentAction() => dirs[len - 1];

        public string Print()
        {
            var result = new StringBuilder();
            for (var i = originalLen - 1; i >= 0; i--)
                result.Append(dirs[i].ToString()[0]);
            return result.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            len = originalLen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            len = 0;
            originalLen = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Direction ApplyNext(State state, int player)
        {
            if (--len >= 0)
                return dirs[len];

            var result = default(Direction);
            var top = state.players[player].dir == null ? 6 : 5;
            for (var sd = 3; sd <= top; sd++)
            {
                var nd = (Direction)(((int)(state.players[player].dir ?? Direction.Up) + sd) % 4);
                var next = state.players[player].arrivePos.NextCoord(nd);
                if (next != ushort.MaxValue)
                {
                    result = nd;
                    if (state.territory[next] == player)
                        break;
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Revert()
        {
            ++len;
        }
    }
}