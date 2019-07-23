using System;
using System.Linq;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastBonus
    {
        public BonusType type;
        public V pos;

        public FastBonus(RequestInput.BonusData inputBonusData, Config config)
        {
            type = inputBonusData.type;
            pos = inputBonusData.position.ToCellCoords(config.width);
        }
    }

    public class FastState
    {
        public int curPlayer;

        public int time;
        public int turn;

        public byte[,] territory;
        public byte[,] lines;

        public int bonusCount;
        public FastBonus[] bonuses;

        public FastPlayer[] players;

        public FastTerritoryCapture capture = new FastTerritoryCapture();

        public void Reset()
        {
            players = null;
        }

        public void SetInput(Config config, RequestInput input)
        {
            if (players == null)
            {
                players = new FastPlayer[input.players.Count];

                territory = new byte[config.x_cells_count, config.y_cells_count];
                lines = new byte[config.y_cells_count, config.x_cells_count];

                curPlayer = (1 + input.players.Count) * input.players.Count / 2
                            - input.players.Sum(x => int.TryParse(x.Key, out var id) ? id : 0)
                            - 1;
            }

            capture.Init(config, players.Length);

            time = input.tick_num;
            turn = 0;

            for (var i = 0; i < players.Length; i++)
            {
                var key = i == curPlayer ? "i" : (i + 1).ToString();

                if (players[i] == null)
                {
                    players[i] = new FastPlayer
                    {
                        line = new V[config.x_cells_count * config.y_cells_count]
                    };
                }

                if (!input.players.TryGetValue(key, out var playerData))
                    players[i].status = PlayerStatus.Eliminated;
                else
                {
                    var accel = 0;
                    if (playerData.bonuses.Any(b => b.type == BonusType.N))
                        accel++;
                    if (playerData.bonuses.Any(b => b.type == BonusType.S))
                        accel--;

                    var speed = accel == 0 ? config.speed
                        : accel == -1 ? config.slowSpeed
                        : accel == 1 ? config.nitroSpeed
                        : throw new InvalidOperationException();

                    var v = playerData.position;
                    var arriveTime = 0;
                    while (!v.InCellCenter(config.width))
                    {
                        v += GetShift(playerData.direction ?? throw new InvalidOperationException(), speed);
                        arriveTime++;
                    }

                    players[i].arrivePos = V.Get(
                        (v.X - config.width / 2) / config.width,
                        (v.Y - config.width / 2) / config.width);
                    if (arriveTime == 0)
                        players[i].pos = players[i].arrivePos;
                    else
                        players[i].pos = players[i].arrivePos - GetShift(playerData.direction ?? throw new InvalidOperationException(), 1);

                    players[i].status = PlayerStatus.Active;
                    players[i].dir = playerData.direction;
                    players[i].score = playerData.score;
                    players[i].shiftTime = accel == 0 ? config.ticksPerRequest
                        : accel == -1 ? config.slowTicksPerRequest
                        : accel == 1 ? config.nitroTicksPerRequest
                        : throw new InvalidOperationException();
                    players[i].arriveTime = arriveTime;
                    players[i].slowLeft = playerData.bonuses.FirstOrDefault(b => b.type == BonusType.S)?.ticks ?? 0;
                    players[i].nitroLeft = playerData.bonuses.FirstOrDefault(b => b.type == BonusType.N)?.ticks ?? 0;
                    players[i].lineCount = playerData.lines.Length;
                    players[i].territory = playerData.territory.Length;

                    for (int x = 0; x < config.x_cells_count; x++)
                    for (int y = 0; y < config.y_cells_count; y++)
                    {
                        territory[x, y] = 0xFF;
                        lines[x, y] = 0;
                    }

                    for (var k = 0; k < playerData.lines.Length; k++)
                    {
                        var lv = V.Get(
                            (playerData.lines[k].X - config.width / 2) / config.width,
                            (playerData.lines[k].Y - config.width / 2) / config.width);
                        players[i].line[k] = lv;
                        lines[lv.X, lv.Y] = (byte)(lines[lv.X, lv.Y] | (1 << i));
                    }

                    for (var k = 0; k < playerData.territory.Length; k++)
                    {
                        var lv = V.Get(
                            (playerData.territory[k].X - config.width / 2) / config.width,
                            (playerData.territory[k].Y - config.width / 2) / config.width);
                        territory[lv.X, lv.Y] = (byte)i;
                    }
                }
            }

            if (bonuses == null || bonuses.Length < input.bonuses.Length)
                bonuses = new FastBonus[input.bonuses.Length];

            bonusCount = 0;
            for (var i = 0; i < input.bonuses.Length; i++)
            {
                bonuses[bonusCount++] = new FastBonus(input.bonuses[i], config);
            }

            V GetShift(Direction direction, int d) =>
                direction == Direction.Up ? V.Get(0, d)
                : direction == Direction.Down ? V.Get(0, -d)
                : direction == Direction.Right ? V.Get(d, 0)
                : direction == Direction.Left ? V.Get(-d, 0)
                : throw new InvalidOperationException($"Unknown direction: {direction}");
        }

        public bool NextTurn(Config config)
        {
            var minArriveTime = int.MaxValue;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].dir == null)
                {
                    minArriveTime = 1;
                    continue;
                }

                if (players[i].arriveTime == 0)
                {
                    players[i].arriveTime = players[i].shiftTime;
                    players[i].arrivePos = players[i].arrivePos + V.vertAndHoriz[(int)players[i].dir];
                }

                if (players[i].arriveTime < minArriveTime)
                    minArriveTime = players[i].arriveTime;
            }

            time += minArriveTime;
            turn++;
            if (time > Env.MAX_TICK_COUNT)
                return true;

            // Move
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;
                players[i].arriveTime -= minArriveTime;
                if (players[i].arriveTime == 0)
                    players[i].pos = players[i].arrivePos;
            }

            // CheckLoss
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arrivePos.X < 0
                    || players[i].arrivePos.Y < 0
                    || players[i].arrivePos.X >= config.x_cells_count
                    || players[i].arrivePos.Y >= config.y_cells_count)
                {
                    players[i].status = PlayerStatus.Loser;
                }

                for (int k = 0; k < players.Length; k++)
                {
                    if (players[k].status == PlayerStatus.Eliminated)
                        continue;

                    if (players[k].arriveTime != 0)
                        continue;

                    if ((lines[players[k].pos.X, players[k].pos.Y] & (1 << i)) != 0)
                    {
                        players[i].status = PlayerStatus.Loser;
                        if (k != i)
                            players[k].score += Env.LINE_KILL_SCORE;
                    }
                }

                if (players[i].status != PlayerStatus.Loser)
                {
                    if (players[i].territory == 0)
                        players[i].status = PlayerStatus.Loser;
                    else
                    {
                        for (int k = 0; k < players.Length; k++)
                        {
                            if (k == i || players[k].status == PlayerStatus.Eliminated)
                                continue;
                            if (players[i].lineCount >= players[k].lineCount)
                            {
                                var collides = false;
                                if (players[i].arrivePos == players[k].arrivePos)
                                    collides = true;
                                else
                                {
                                    if (players[i].arrivePos == players[k].pos)
                                    {
                                        if (players[i].dir != players[k].dir)
                                            collides = true;
                                        else
                                        {
                                            var distArrive1 = config.width / players[i].shiftTime * players[i].arriveTime;
                                            var distArrive2 = config.width / players[k].shiftTime * players[k].arriveTime;
                                            collides = distArrive1 < distArrive2;
                                        }
                                    }
                                    else if (players[k].arrivePos == players[i].pos)
                                    {
                                        if (players[k].dir != players[i].dir)
                                            collides = true;
                                        else
                                        {
                                            var distArrive2 = config.width / players[k].shiftTime * players[k].arriveTime;
                                            var distArrive1 = config.width / players[i].shiftTime * players[i].arriveTime;
                                            collides = distArrive2 < distArrive1;
                                        }
                                    }
                                }

                                if (collides)
                                    players[i].status = PlayerStatus.Loser;
                            }
                        }
                    }
                }
            }

            // Main
            capture.Clear();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0)
                {
                    // UpdateLines
                    players[i].UpdateLines(i, this);

                    // Capture
                    capture.Capture(this);
                    if (capture.territoryCaptureCount[i] > 0)
                    {
                        players[i].lineCount = 0;
                        players[i].score += Env.NEUTRAL_TERRITORY_SCORE * capture.territoryCaptureCount[i];
                    }
                }
            }

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0)
                {
                    players[i].TickAction(config);

                    for (int b = 0; b < bonusCount; b++)
                    {
                        if (bonuses[b].pos == players[i].pos || capture.BelongsTo(bonuses[b].pos, i))
                        {
                            if (bonuses[b].type == BonusType.N)
                            {
                                if (players[i].nitroLeft > 0)
                                    players[i].nitroLeft += 30; // random 10..50
                                else
                                    players[i].nitroLeft = 30;
                                players[i].UpdateBonusEffect(config);
                            }
                            else if (bonuses[b].type == BonusType.S)
                            {
                                if (players[i].slowLeft > 0)
                                    players[i].slowLeft += 30; // random 10..50
                                else
                                    players[i].slowLeft = 30;
                                players[i].UpdateBonusEffect(config);
                            }
                            else if (bonuses[b].type == BonusType.Saw)
                            {
                                for (int k = 0; k < players.Length; k++)
                                {
                                    if (k == i || players[k].status == PlayerStatus.Eliminated)
                                        continue;

                                    var sawKill = false;
                                    switch (players[i].dir.Value)
                                    {
                                        case Direction.Up:
                                            if (players[k].pos.X == players[i].pos.X && players[k].pos.Y > players[i].pos.Y
                                                || players[k].arrivePos.X == players[i].pos.X && players[k].arrivePos.Y > players[i].pos.Y)
                                                sawKill = true;
                                            break;
                                        case Direction.Left:
                                            if (players[k].pos.Y == players[i].pos.Y && players[k].pos.X < players[i].pos.X
                                                || players[k].arrivePos.Y == players[i].pos.Y && players[k].arrivePos.X < players[i].pos.X)
                                                sawKill = true;
                                            break;
                                        case Direction.Down:
                                            if (players[k].pos.X == players[i].pos.X && players[k].pos.Y < players[i].pos.Y
                                                || players[k].arrivePos.X == players[i].pos.X && players[k].arrivePos.Y < players[i].pos.Y)
                                                sawKill = true;
                                            break;
                                        case Direction.Right:
                                            if (players[k].pos.Y == players[i].pos.Y && players[k].pos.X > players[i].pos.X
                                                || players[k].arrivePos.Y == players[i].pos.Y && players[k].arrivePos.X > players[i].pos.X)
                                                sawKill = true;
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                    
                                    if (sawKill)
                                    {
                                        players[k].status = PlayerStatus.Loser;
                                        players[i].score += Env.SAW_KILL_SCORE;
                                    }
                                    else
                                    {
                                        // todo 
                                        /*
                                         * var removed = p.Territory.Split(line, player.Dir ?? throw new InvalidOperationException(), p);
                                            if (removed.Count > 0)
                                                player.Score += Env.SAW_SCORE;
                                         */
                                    }
                                }
                            }

                            bonuses[b] = bonuses[bonusCount-- - 1];
                        }
                    }
                    
                    capture.ApplyTo(this);
                }
            }

            var playersLeft = 0;
            for (int i = 0; i < players.Length; i++)
            {
                switch (players[i].status)
                {
                    case PlayerStatus.Loser:
                        players[i].status = PlayerStatus.Eliminated;
                        break;
                    case PlayerStatus.Active:
                        playersLeft++;
                        break;
                }
            }

            return playersLeft > 0;
        }
    }
}