using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace BrutalTester.Sim
{
    public class Game
    {
        private static readonly Func<V, Bonus>[] availableBonuses =
        {
            v => new Nitro(v),
            v => new Slowdown(v),
            v => new Saw(v)
        };

        public List<Player> Players { get; } = new List<Player>();
        public List<Player> Losers { get; } = new List<Player>();
        public List<Bonus> Bonuses { get; } = new List<Bonus>();
        public int Tick { get; set; }

        public Game(IClient[] clients)
        {
            var coordinates = GetCoordinates(clients.Length);
            for (var i = 0; i < clients.Length; i++)
                Players.Add(new Player(i + 1, coordinates[i], clients[i]));
            Tick = 1;
        }

        private static V[] GetCoordinates(int clientCount)
        {
            var dx = Env.X_CELLS_COUNT / 6 * Env.WIDTH;
            var dy = Env.Y_CELLS_COUNT / 6 * Env.WIDTH;

            V[] coords;

            switch (clientCount)
            {
                case 1:
                    coords = new[] {V.Get(3 * dx, 3 * dy)};
                    break;
                case 2:
                    coords = new[]
                    {
                        V.Get(2 * dx, 3 * dy),
                        V.Get(4 * dx, 3 * dy)
                    };
                    break;
                case 3:
                case 4:
                    coords = new[]
                    {
                        V.Get(2 * dx, 2 * dy),
                        V.Get(2 * dx, 4 * dy),
                        V.Get(4 * dx, 2 * dy),
                        V.Get(4 * dx, 4 * dy),
                    };
                    break;
                default:
                    var x = Env.X_CELLS_COUNT / 5 * Env.WIDTH;
                    var y = (Env.WINDOW_HEIGHT + Env.WINDOW_WIDTH - 4 * x) / 3;
                    var b = (Env.WINDOW_WIDTH - 2 * x) / 2;
                    var a = y - b;

                    coords = new[]
                    {
                        V.Get(x, x + a),
                        V.Get(x, x + a + y + Env.WIDTH),

                        V.Get(Env.WINDOW_WIDTH / 2, Env.WINDOW_HEIGHT - x + Env.WIDTH),
                        V.Get(Env.WINDOW_WIDTH / 2, x),

                        V.Get(Env.WINDOW_WIDTH - x + Env.WIDTH, x + a),
                        V.Get(Env.WINDOW_WIDTH - x + Env.WIDTH, x + a + y + Env.WIDTH),
                    };

                    break;
            }

            coords = coords
                .Select(
                    v => V.Get(
                        v.X / Env.WIDTH * Env.WIDTH - Env.WIDTH / 2,
                        v.Y / Env.WIDTH * Env.WIDTH - Env.WIDTH / 2))
                .ToArray();
            return coords;
        }

        public void Play()
        {
            SendGameStart();
            while (true)
            {
                var isGameOver = GameLoop();
                if (isGameOver || Tick > Env.MAX_TICK_COUNT)
                {
                    SendGameEnd();
                    break;
                }
            }
        }

        private bool GameLoop()
        {
            foreach (var player in Players)
            {
                if (player.Pos.InCellCenter(Env.WIDTH))
                {
                    var direction = SendTick(player);
                    player.ChangeDirection(direction);
                }
            }

            foreach (var player in Players)
                player.Move();

            var playerToCaptured = new Dictionary<Player, HashSet<V>>();
            foreach (var player in Players)
            {
                player.RemoveSawBonus();
                if (player.Pos.InCellCenter(Env.WIDTH))
                {
                    player.UpdateLines();
                    var captured = player.Territory.Capture(player.Lines);
                    playerToCaptured[player] = captured;
                    if (captured.Count > 0)
                    {
                        player.Lines.Clear();
                        player.TickScore += Env.NEUTRAL_TERRITORY_SCORE * captured.Count;
                    }
                }
            }
            
            foreach (var player in Players)
            {
                if (CheckLoss(player))
                    Losers.Add(player);
            }

            playerToCaptured = CollisionResolution(playerToCaptured);
            
            foreach (var player in Players)
            {
                if (player.IsAte(playerToCaptured))
                    Losers.Add(player);
            }

            foreach (var player in Players)
            {
                if (player.Pos.InCellCenter(Env.WIDTH))
                {
                    if (!playerToCaptured.TryGetValue(player, out var captured))
                        captured = new HashSet<V>();
                
                    player.TickAction();

                    foreach (var bonus in Bonuses.ToList())
                    {
                        if (bonus.IsAte(player, captured))
                        {
                            bonus.Apply(player);
                            Bonuses.Remove(bonus);

                            if (bonus is Saw)
                            {
                                var line = player.GetDirectionLine();
                                foreach (var p in Players)
                                {
                                    if (p != player)
                                    {
                                        if (line.Any(point => p.Pos.IntersectsWith(point, Env.WIDTH)))
                                        {
                                            Losers.Add(p);
                                            player.TickScore += Env.SAW_KILL_SCORE;
                                        }
                                        else
                                        {
                                            var removed = p.Territory.Split(line, player.Dir ?? throw new InvalidOperationException(), p);
                                            if (removed.Count > 0)
                                                player.TickScore += Env.SAW_SCORE;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (captured.Count > 0)
                    {
                        player.Territory.Points.UnionWith(captured);
                        foreach (var p in Players)
                        {
                            if (p != player)
                            {
                                var removed = p.Territory.RemovePoints(captured);
                                player.TickScore += (Env.ENEMY_TERRITORY_SCORE - Env.NEUTRAL_TERRITORY_SCORE) * removed.Count;
                            }
                        }
                    }
                }
            }

            foreach (var loser in Losers)
                Players.Remove(loser);
            
            foreach (var player in Players)
            {
                player.Score += player.TickScore;
                player.TickScore = 0;
            }

            GenerateBonus();

            Tick++;
            return Players.Count == 0;
        }

        private Dictionary<Player, HashSet<V>> CollisionResolution(Dictionary<Player,HashSet<V>> playerToCaptured)
        {
            var p_to_c = playerToCaptured.Where(x => !x.Key.IsAte(playerToCaptured)).ToDictionary(x => x.Key, x => x.Value);
            var res = p_to_c.ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvp1 in playerToCaptured)
            foreach (var kvp2 in playerToCaptured)
            {
                if (kvp1.Key != kvp2.Key)
                    res[kvp1.Key].ExceptWith(kvp2.Value);
            }
            return res;
        }

        private Direction SendTick(Player player)
        {
            return player.Client.SendRequestInput(
                    new RequestInput
                    {
                        tick_num = Tick,
                        players = Players.ToDictionary(
                            x => x == player ? "i" : x.Id.ToString(),
                            x => new RequestInput.PlayerData
                            {
                                position = x.Pos,
                                lines = x.Lines.ToArray(),
                                territory = x.Territory.Points.ToArray(),
                                direction = x.Dir,
                                score = x.Score,
                                bonuses = x.Bonuses.Select(
                                        b => new RequestInput.BonusState
                                        {
                                            type = b.Type,
                                            ticks = b.RemainingTicks
                                        })
                                    .ToArray()
                            }),
                        bonuses = Bonuses.Select(
                                x => new RequestInput.BonusData
                                {
                                    position = x.Pos,
                                    type = x.Type
                                })
                            .ToArray()
                    })
                .Command;
        }

        private void SendGameStart()
        {
            foreach (var player in Players)
            {
                player.Client.SendConfig(
                    new Config
                    {
                        speed = Env.SPEED,
                        width = Env.WIDTH,
                        x_cells_count = Env.X_CELLS_COUNT,
                        y_cells_count = Env.Y_CELLS_COUNT
                    });
            }
        }

        private void SendGameEnd()
        {
            foreach (var player in Players)
                player.Client.SendGameEnd();
        }

        private void GenerateBonus()
        {
            if (Helpers.RandInt(1, Env.BONUS_CHANCE) == 1 && Bonuses.Count < Env.BONUSES_MAX_COUNT)
            {
                var pos = Bonus.GenerateCoordinates(Players, GetBusyPoints());
                var bonus = availableBonuses.RandArrayItem().Invoke(pos);
                Bonuses.Add(bonus);
            }
        }

        private HashSet<V> GetBusyPoints()
        {
            return new HashSet<V>(
                Players.Select(x => x.Pos)
                    .Concat(Bonuses.Select(x => x.Pos))
                    .Concat(Players.SelectMany(x => x.Lines)));
        }

        private bool CheckLoss(Player player)
        {
            var isLoss = false;
            if (player.Pos.X < Env.WIDTH / 2)
                isLoss = true;
            else if (player.Pos.Y < Env.WIDTH / 2)
                isLoss = true;
            else if (player.Pos.X > Env.WINDOW_WIDTH - Env.WIDTH / 2)
                isLoss = true;
            else if (player.Pos.Y > Env.WINDOW_HEIGHT - Env.WIDTH / 2)
                isLoss = true;

            foreach (var p in Players)
            {
                if (player.Lines.Take(player.Lines.Count - 1).Contains(p.Pos))
                {
                    if (p != player)
                        p.TickScore += Env.LINE_KILL_SCORE;
                    isLoss = true;
                }
            }

            foreach (var p in Players)
            {
                if (p != player && p.Pos.IntersectsWith(player.Pos, Env.WIDTH))
                {
                    if (player.Lines.Count >= p.Lines.Count)
                        isLoss = true;
                }
            }

            if (player.Territory.Points.Count == 0)
                isLoss = true;

            return isLoss;
        }
    }
}