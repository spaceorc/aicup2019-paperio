using System.IO;
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
        public readonly int[] places = new int[6];
        public readonly int[] potentialScores = new int[6];
        
        private readonly StateBackup backup = new StateBackup();
        private readonly Direction[] commands = new Direction[6];
        private readonly int[] distCount = new int[6];

        public void Build(State state, DistanceMap distanceMap)
        {
            for (int i = 0; i < state.players.Length; i++)
            {
                places[i] = i;
                potentialScores[i] = 0;
            }

            for (int i = 0; i < state.players.Length - 1; i++)
            {
                for (int k = i + 1; k < state.players.Length; k++)
                {
                    if (state.players[places[i]].score < state.players[places[k]].score)
                    {
                        var tmp = places[i];
                        places[i] = places[k];
                        places[k] = tmp;
                    }
                }
            }

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
            while (!state.isGameOver)
            {
                var ended = 0;
                for (var i = 0; i < state.players.Length; i++)
                {
                    if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                    {
                        ended++;
                        continue;
                    }

                    if (state.players[i].arriveTime != 0)
                    {
                        if (pathsToOwned[i].len < 0)
                            ended++;
                        continue;
                    }

                    if (pathsToOwned[i].len <= 0)
                        ended++;

                    commands[i] = pathsToOwned[i].ApplyNext(state, i);
                    distCount[i]++;
                }

                if (ended == state.players.Length)
                {
                    for (int i = 0; i < state.players.Length; i++)
                        potentialScores[i] = state.players[i].score - backup.players[i].score;
                    break;
                }
                
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

        public string PrintTerritoryTtl(State state)
        {
            var players = state.players;
            var bonuses = state.bonuses;
            var bonusCount = state.bonusCount;
            var lines = state.lines;
            var territory = state.territory;
            const string tc = "ABCDEF";
            using (var writer = new StringWriter())
            {
                for (var y = Env.Y_CELLS_COUNT - 1; y >= 0; y--)
                {
                    for (var x = 0; x < Env.X_CELLS_COUNT; x++)
                    {
                        var c = (ushort)(y * Env.X_CELLS_COUNT + x);

                        var dist = territoryTtl[c];

                        var player = -1;
                        for (var p = 0; p < players.Length; p++)
                        {
                            if (players[p].status != PlayerStatus.Eliminated && (players[p].pos == c || players[p].arrivePos == c))
                            {
                                player = p;
                                break;
                            }
                        }

                        Bonus bonus = null;
                        for (var b = 0; b < bonusCount; b++)
                        {
                            if (bonuses[b].pos == c)
                            {
                                bonus = bonuses[b];
                                break;
                            }
                        }

                        if (bonus?.type == BonusType.N)
                            writer.Write('N');
                        else if (bonus?.type == BonusType.S)
                            writer.Write('S');
                        else if (bonus?.type == BonusType.Saw)
                            writer.Write('W');
                        else if (player != -1)
                            writer.Write(player);
                        else if (lines[c] != 0)
                            writer.Write('x');
                        else if (territory[c] == 0xFF)
                            writer.Write('.');
                        else
                            writer.Write(tc[territory[c]]);

                        if (dist == int.MaxValue)
                            writer.Write("     ");
                        else
                            writer.Write($"_{dist.ToString("").PadRight(3)} ");
                    }

                    writer.WriteLine();
                }

                return writer.ToString();
            }
        }
    }
}