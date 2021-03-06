using System;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk.StartPathStrategies
{
    public class NearestOpponentStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(State state, int player, byte allowedDirectionsMask, DistanceMap distanceMap)
        {
            var target = distanceMap.nearestOpponentOwned[player];
            if (target == ushort.MaxValue)
            {
                target = distanceMap.nearestEmpty[player];
                if (target == ushort.MaxValue)
                    throw new InvalidOperationException("Couldn't find nearest to conquer");
            }

            var next = target;
            for (var cur = target; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths1[player, cur])
                next = cur;

            if (next != target && state.territory[next] == player)
            {
                var dirTo = state.players[player].arrivePos.DirTo(next);
                if ((allowedDirectionsMask & (1 << (int)dirTo)) != 0)
                    return new RequestOutput {Command = dirTo, Debug = $"Goto nearest {state.players[player].arrivePos}->{target}"};
            }

            return null;
        }
    }
}