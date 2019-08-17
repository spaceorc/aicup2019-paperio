using System;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies.RandomWalk.StartPathStrategies
{
    public class SafestOpponentStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(State state, int player, byte allowedDirectionsMask, DistanceMap distanceMap)
        {
            var bestTarget = ushort.MaxValue;
            var bestEstimation = int.MinValue; 
            var bestTime = int.MaxValue; 
            for (ushort target = 0; target < Env.CELLS_COUNT; target++)
            {
                if (state.territory[target] != 0xFF && state.territory[target] != player)
                {
                    for (var d = 0; d < 4; d++)
                    {
                        var ne = target.NextCoord((Direction)d);
                        if (ne != ushort.MaxValue)
                        {
                            if (state.territory[ne] == player)
                            {
                                var estimation = Estimate(state, player, distanceMap, target);
                                if (estimation > bestEstimation || estimation == bestEstimation && distanceMap.times[player, target] < bestTime)
                                {
                                    bestEstimation = estimation;
                                    bestTarget = target;
                                    bestTime = distanceMap.times[player, target];
                                }
                            }
                        }
                    }
                }
            }
            
            if (bestTarget == ushort.MaxValue)
            {
                bestTarget = distanceMap.nearestEmpty[player];
                if (bestTarget == ushort.MaxValue)
                    throw new InvalidOperationException("Couldn't find nearest to conquer");
            }

            var next = bestTarget;
            for (var cur = bestTarget; cur != state.players[player].arrivePos; cur = (ushort)distanceMap.paths[player, cur])
                next = cur;

            if (next != bestTarget && state.territory[next] == player)
                return new RequestOutput {Command = state.players[player].arrivePos.DirTo(next), Debug = $"Goto nearest {state.players[player].arrivePos}->{bestTarget} with estimation {bestEstimation}"};

            return null;
        }

        private int Estimate(State state, int player, DistanceMap distanceMap, ushort target)
        {
            //var gainTime = distanceMap.times[player, target];
            var minTime = int.MaxValue;
            for (var other = 0; other < state.players.Length; other++)
            {
                if (other == player || state.players[other].status == PlayerStatus.Eliminated)
                    continue;

                if (distanceMap.times[other, target] != -1 && distanceMap.times[other, target] < minTime)
                    minTime = distanceMap.times[other, target];
            }

            if (minTime > 10 * Env.TICKS_PER_REQUEST)
                minTime = 10 * Env.TICKS_PER_REQUEST;

            return minTime;
        }
    }
}