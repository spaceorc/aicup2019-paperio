using System.Linq;
using Game.Protocol;
using Game.Sim;
using Game.Sim.Undo;

namespace Game.Strategies.RandomWalk
{
    public class InterestingFacts
    {
        public readonly PlayerPath[] pathsToOwned = Enumerable.Repeat(0, 6).Select(x => new PlayerPath()).ToArray();
        public readonly int[] territoryTtl = new int[Env.CELLS_COUNT];
        public readonly int[] sawCollectTime = new int[6];
        public readonly int[] sawCollectDistance = new int[6];
        
        private readonly StateBackup backup = new StateBackup();
        private readonly Direction[] commands = new Direction[6];
        private readonly int[] distCount = new int[6];

        public void Build(State state, DistanceMap distanceMap)
        {
            for (int i = 0; i < state.players.Length; i++)
            {
                if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                    continue;
                sawCollectTime[i] = int.MaxValue;
                sawCollectDistance[i] = int.MaxValue;
                distCount[i] = 0;
                pathsToOwned[i].BuildPath(state, distanceMap, i, distanceMap.nearestOwned[i]);
            }

            for (ushort c = 0; c < Env.CELLS_COUNT; ++c)
                territoryTtl[c] = int.MaxValue;

            for (int b = 0; b < state.bonusCount; b++)
            {
                if (state.bonuses[b].type == BonusType.Saw)
                {
                    for (var i = 0; i < state.players.Length; i++)
                    {
                        if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                            continue;
                        var st = distanceMap.times[i, state.bonuses[b].pos];
                        if (st != -1 && st != int.MaxValue)
                        {
                            sawCollectTime[i] = st;
                            sawCollectDistance[i] = distanceMap.distances[i, state.bonuses[b].pos];
                        } 
                    }
                }
            }

            var territoryVersion = state.territoryVersion;
            backup.Backup(state);
            while (true)
            {
                var ended = true;
                for (var i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                        continue;
                    if (state.players[i].arriveTime != 0)
                        continue;

                    if (pathsToOwned[i].len > 0)
                        ended = false;

                    commands[i] = pathsToOwned[i].ApplyNext(state, i);
                    distCount[i]++;
                }
                
                if (ended)
                    break;
                
                state.NextTurn(commands, false);
                if (state.territoryVersion != territoryVersion)
                {
                    territoryVersion = state.territoryVersion;
                    for (ushort c = 0; c < Env.CELLS_COUNT; ++c)
                    {
                        if (territoryTtl[c] == int.MaxValue && state.territory[c] != backup.territory[c])
                            territoryTtl[c] = state.time - backup.time;
                    }
                }

                for (var i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                        continue;
                    if (state.players[i].sawsCollected > 0)
                    {
                        if (state.time - backup.time < sawCollectTime[i])
                            sawCollectTime[i] = state.time - backup.time;
                        if (distCount[i] < sawCollectDistance[i])
                            sawCollectDistance[i] = distCount[i];
                    }
                }

            }
            
            
            backup.Restore(state);

            for (var i = 0; i < state.players.Length; i++)
                pathsToOwned[i].Reset();
        }
    }
}