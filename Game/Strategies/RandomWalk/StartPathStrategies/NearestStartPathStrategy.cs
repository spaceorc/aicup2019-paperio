using System;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk.StartPathStrategies
{
    public class NearestStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(State state, int player, DistanceMap distanceMap)
        {
            var empty = distanceMap.nearestEmpty[player];
            if (empty == ushort.MaxValue)
                throw new InvalidOperationException("Couldn't find nearest to conquer");

            var next = empty;
            for (var cur = empty; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                next = cur;

            if (next != empty)
                return new RequestOutput {Command = state.players[player].arrivePos.DirTo(next), Debug = $"Goto nearest {state.players[player].arrivePos}->{empty}"};

            return null;
        }
    }
}