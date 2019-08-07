using System;
using Game.Fast;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public class SafestOpponentStartPathStrategy : IStartPathStrategy
    {
        public RequestOutput GotoStart(FastState state, int player, DistanceMapGenerator distanceMap)
        {
            var bestTarget = ushort.MaxValue;
            int bestEstimation = int.MinValue; 
            int bestTime = int.MaxValue; 
            for (ushort target = 0; target < state.config.x_cells_count * state.config.y_cells_count; target++)
            {
                if (state.territory[target] != 0xFF && state.territory[target] != player)
                {
                    for (int d = 0; d < 4; d++)
                    {
                        var ne = state.NextCoord(target, (Direction)d);
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
                return new RequestOutput {Command = state.MakeDir(state.players[player].arrivePos, next), Debug = $"Goto nearest {state.players[player].arrivePos}->{bestTarget} with estimation {bestEstimation}"};

            return null;
        }

        private int Estimate(FastState state, int player, DistanceMapGenerator distanceMap, ushort target)
        {
            //var gainTime = distanceMap.times[player, target];
            var minTime = int.MaxValue;
            for (int other = 0; other < state.players.Length; other++)
            {
                if (other == player || state.players[other].status == PlayerStatus.Eliminated)
                    continue;

                if (distanceMap.times[other, target] < minTime)
                    minTime = distanceMap.times[other, target];
            }

            if (minTime > 10 * state.config.ticksPerRequest)
                minTime = 10 * state.config.ticksPerRequest;

            return minTime;
        }
    }
}