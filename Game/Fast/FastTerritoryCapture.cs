using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastTerritoryCapture
    {
        public int gen;
        public int[,] territoryCaptureMask;

        public int[] territoryCaptureCount;
        public V[,] territoryCapture;

        public void Init(Config config, int playerCount)
        {
            gen = 0;
            if (territoryCaptureMask == null
                || territoryCaptureMask.GetLength(0) != config.x_cells_count
                || territoryCaptureMask.GetLength(1) != config.y_cells_count)
            {
                territoryCaptureMask = new int[config.x_cells_count, config.y_cells_count];
            }
            else
            {
                for (int x = 0; x < config.x_cells_count; x++)
                for (int y = 0; y < config.y_cells_count; y++)
                {
                    territoryCaptureMask[x, y] = 0;
                }
            }

            if (territoryCaptureCount == null || territoryCaptureCount.Length != playerCount)
            {
                territoryCaptureCount = new int[playerCount];
            }
            else
            {
                for (int i = 0; i < territoryCaptureCount.Length; i++)
                    territoryCaptureCount[i] = 0;
            }

            if (territoryCapture == null
                || territoryCapture.GetLength(0) != playerCount
                || territoryCapture.GetLength(1) != config.x_cells_count * config.y_cells_count)
            {
                territoryCapture = new V[playerCount, config.x_cells_count * config.y_cells_count];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            gen += 1 << 8;
            for (int i = 0; i < territoryCaptureCount.Length; i++)
                territoryCaptureCount[i] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BelongsTo(V v, int player)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(V v, int player)
        {
            var mask = territoryCaptureMask[v.X, v.Y];
            if ((mask & ~0xFF) == gen)
            {
                if ((mask & (1 << player)) == 0)
                {
                    territoryCaptureMask[v.X, v.Y] = mask | (1 << player);
                    territoryCapture[player, territoryCaptureCount[player]++] = v;
                }
            }
            else
            {
                territoryCaptureMask[v.X, v.Y] = (1 << player) | gen;
                territoryCapture[player, territoryCaptureCount[player]++] = v;
            }
        }

        public void ApplyTo(FastState state)
        {
            // todo 
            /*if (captured.Count > 0)
                    {
                        player.Territory.Points.UnionWith(captured);
                        foreach (var p in Players)
                        {
                            if (p != player)
                            {
                                var removed = p.Territory.RemovePoints(captured);
                                player.Score += (Env.ENEMY_TERRITORY_SCORE - Env.NEUTRAL_TERRITORY_SCORE) * removed.Count;
                            }
                        }
                    }*/
            for (int player = 0; player < territoryCaptureCount.Length; player++)
            {
                for (int i = 0; i < territoryCaptureCount[i]; i++)
                {
                    var v = territoryCapture[player, i];
                    var mask = territoryCaptureMask[v.X, v.Y] & 0xFF;
                    if ((mask & ~(1 << player)) == 0)
                        state.territory[v.X, v.Y] = (byte)player;
                }
            }
        }

        public void Capture(FastState state)
        {
            for (int i = 0; i < state.players.Length; i++)
            {
                
            }
        }
    }
}