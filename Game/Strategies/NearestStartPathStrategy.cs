using System;
using Game.Fast;
using Game.Protocol;

namespace Game.Strategies
{
    public class NearestStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(FastState state, int player, DistanceMapGenerator distanceMap)
        {
            var empty = distanceMap.nearestEmpty[player];
            if (empty == ushort.MaxValue)
                throw new InvalidOperationException("Couldn't find nearest to conquer");

            var next = empty;
            for (var cur = empty; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                next = cur;

            if (next != empty)
                return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, next), Debug = $"Goto nearest {state.players[player].arrivePos}->{empty}"};

            return null;
        }
    }
}