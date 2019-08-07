using System;
using Game.Fast;
using Game.Protocol;

namespace Game.Strategies
{
    public class NearestOpponentStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(FastState state, int player, DistanceMapGenerator distanceMap)
        {
            var target = distanceMap.nearestOpponent[player];
            if (target == ushort.MaxValue)
            {
                target = distanceMap.nearestEmpty[player];
                if (target == ushort.MaxValue)
                    throw new InvalidOperationException("Couldn't find nearest to conquer");
            }

            var next = target;
            for (var cur = target; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                next = cur;

            if (next != target && state.territory[next] == player)
                return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, next), Debug = $"Goto nearest {state.players[player].arrivePos}->{target}"};

            return null;
        }
    }
}