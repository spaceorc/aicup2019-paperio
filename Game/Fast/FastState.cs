using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastState
    {
        public Config config;

        public bool isGameOver;

        public int curPlayer;

        public int time;

        public int territoryVersion;
        public byte[] territory;
        public byte[] lines;

        public int bonusCount;
        public FastBonus[] bonuses;

        public FastPlayer[] players;

        public FastTerritoryCapture capture = new FastTerritoryCapture();

        public UndoDataPool undos;

        public void Reset()
        {
            players = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort NextCoord(ushort prev, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    var result = prev - config.x_cells_count;
                    if (result < 0)
                        return ushort.MaxValue;
                    return (ushort)result;

                case Direction.Left:
                    if (prev % config.x_cells_count == 0)
                        return ushort.MaxValue;
                    return (ushort)(prev - 1);

                case Direction.Down:
                    result = prev + config.x_cells_count;
                    if (result >= config.x_cells_count * config.y_cells_count)
                        return ushort.MaxValue;
                    return (ushort)result;

                case Direction.Right:
                    if (prev % config.x_cells_count == config.x_cells_count - 1)
                        return ushort.MaxValue;
                    return (ushort)(prev + 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        public ushort ToCoord(V v)
        {
            return (ushort)(v.X + v.Y * config.x_cells_count);
        }

        public void SetInput(Config config, RequestInput input)
        {
            if (players == null)
            {
                this.config = config;
                players = new FastPlayer[input.players.Count];

                territory = new byte[config.x_cells_count * config.y_cells_count];
                lines = new byte[config.y_cells_count * config.x_cells_count];

                curPlayer = (1 + input.players.Count) * input.players.Count / 2
                            - input.players.Sum(x => int.TryParse(x.Key, out var id) ? id : 0)
                            - 1;

                undos = new UndoDataPool(input.players.Count, config);
            }

            isGameOver = false;

            capture.Init(config, players.Length);

            time = input.tick_num;

            for (int c = 0; c < config.x_cells_count * config.y_cells_count; c++)
            {
                territory[c] = 0xFF;
                lines[c] = 0;
            }

            for (var i = 0; i < players.Length; i++)
            {
                var key = i == curPlayer ? "i" : (i + 1).ToString();

                if (players[i] == null)
                {
                    players[i] = new FastPlayer(config);
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

                    players[i].arrivePos = ToCoord(v.ToCellCoords(config.width));
                    if (arriveTime == 0)
                        players[i].pos = players[i].arrivePos;
                    else
                        players[i].pos = NextCoord(players[i].arrivePos, (playerData.direction ?? throw new InvalidOperationException()) + 2);

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

                    for (var k = 0; k < playerData.lines.Length; k++)
                    {
                        var lv = ToCoord(playerData.lines[k].ToCellCoords(config.width));
                        players[i].line[k] = lv;
                        lines[lv] = (byte)(lines[lv] | (1 << i));
                    }

                    for (var k = 0; k < playerData.territory.Length; k++)
                    {
                        var lv = ToCoord(playerData.territory[k].ToCellCoords(config.width));
                        territory[lv] = (byte)i;
                    }
                }
            }

            if (bonuses == null || bonuses.Length < input.bonuses.Length)
                bonuses = new FastBonus[input.bonuses.Length];

            bonusCount = 0;
            for (var i = 0; i < input.bonuses.Length; i++)
            {
                bonuses[bonusCount++] = new FastBonus(this, input.bonuses[i], config);
            }

            V GetShift(Direction direction, int d) =>
                direction == Direction.Up ? V.Get(0, d)
                : direction == Direction.Down ? V.Get(0, -d)
                : direction == Direction.Right ? V.Get(d, 0)
                : direction == Direction.Left ? V.Get(-d, 0)
                : throw new InvalidOperationException($"Unknown direction: {direction}");
        }

        public void Undo(UndoData undo)
        {
            undo.Undo(this);
            undos.Return(undo);
        }

        public UndoData NextTurn(Direction[] commands)
        {
            var undo = undos.Get();
            undo.Before(this);

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0)
                    players[i].dir = commands[i];
            }

            var timeDelta = RenewArriveTime();

            time += timeDelta;
            if (time > Env.MAX_TICK_COUNT)
            {
                isGameOver = true;
                undo.After(this);
                return undo;
            }

            Move(timeDelta);

            // Main
            capture.Clear();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue)
                {
                    // Capture
                    capture.Capture(this, i, config);
                    if (capture.territoryCaptureCount[i] > 0)
                    {
                        for (int l = 0; l < players[i].lineCount; l++)
                        {
                            var lv = players[i].line[l];
                            lines[lv] = (byte)(lines[lv] & ~(1 << i));
                        }

                        players[i].lineCount = 0;
                        players[i].tickScore += Env.NEUTRAL_TERRITORY_SCORE * capture.territoryCaptureCount[i];
                    }
                }
            }

            CheckLoss(config);

            CheckIsAte();

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue)
                {
                    players[i].TickAction(config);

                    for (int b = 0; b < bonusCount;)
                    {
                        if (bonuses[b].pos != players[i].arrivePos && !capture.BelongsTo(bonuses[b].pos, i))
                            b++;
                        else
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
                                var sawStatus = 0L;
                                var v = players[i].arrivePos;
                                while (true)
                                {
                                    v = NextCoord(v, players[i].dir.Value);
                                    if (v == ushort.MaxValue)
                                        break;
                                    for (int k = 0; k < players.Length; k++)
                                    {
                                        if (k == i || players[k].status == PlayerStatus.Eliminated)
                                            continue;
                                        if (players[k].arrivePos == v || (players[k].pos == v && players[k].arriveTime > 0))
                                        {
                                            sawStatus |= 0xFF << (k * 8);
                                            players[k].status = PlayerStatus.Loser;
                                            players[i].tickScore += Env.SAW_KILL_SCORE;
                                        }
                                    }

                                    var owner = territory[v];
                                    if (owner != 0xFF && owner != i)
                                    {
                                        if (players[owner].status != PlayerStatus.Eliminated)
                                            sawStatus |= 1 << (owner * 8);
                                    }
                                }

                                for (int k = 0; k < players.Length; k++)
                                {
                                    if (k == i || players[k].status == PlayerStatus.Eliminated)
                                        continue;

                                    if (((sawStatus >> (k * 8)) & 0xFF) != 1)
                                        continue;

                                    players[i].tickScore += Env.SAW_SCORE;
                                    var vx = v % config.x_cells_count;
                                    var vy = v / config.x_cells_count;
                                    if (players[i].dir.Value == Direction.Up || players[i].dir.Value == Direction.Down)
                                    {
                                        if (players[k].arrivePos % config.x_cells_count < vx)
                                        {
                                            int pos = 0;
                                            for (int y = 0; y < config.y_cells_count; y++)
                                            {
                                                pos += vx;
                                                for (int x = vx; x < config.x_cells_count; x++, pos++)
                                                {
                                                    if (territory[pos] == k)
                                                    {
                                                        players[k].territory--;
                                                        territory[pos] = 0xFF;
                                                        territoryVersion++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int pos = 0;
                                            for (int y = 0; y < config.y_cells_count; y++)
                                            {
                                                for (int x = 0; x <= vx; x++, pos++)
                                                {
                                                    if (territory[pos] == k)
                                                    {
                                                        players[k].territory--;
                                                        territory[pos] = 0xFF;
                                                        territoryVersion++;
                                                    }
                                                }
                                                pos += config.x_cells_count - vx - 1;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (players[k].arrivePos / config.x_cells_count < vy)
                                        {
                                            int pos = vy * config.x_cells_count;
                                            for (int y = vy; y < config.y_cells_count; y++)
                                            for (int x = 0; x < config.x_cells_count; x++, pos++)
                                            {
                                                if (territory[pos] == k)
                                                {
                                                    players[k].territory--;
                                                    territory[pos] = 0xFF;
                                                    territoryVersion++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int pos = 0;
                                            for (int y = 0; y <= vy; y++)
                                            for (int x = 0; x < config.x_cells_count; x++, pos++)
                                            {
                                                if (territory[pos] == k)
                                                {
                                                    players[k].territory--;
                                                    territory[pos] = 0xFF;
                                                    territoryVersion++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (b != bonusCount - 1)
                            {
                                var tmp = bonuses[b];
                                bonuses[b] = bonuses[bonusCount - 1];
                                bonuses[bonusCount - 1] = tmp;
                            }

                            bonusCount--;
                        }
                    }

                    capture.ApplyTo(this);
                }
            }

            MoveDone();

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
                        players[i].score += players[i].tickScore;
                        break;
                }

                players[i].tickScore = 0;
            }

            isGameOver = playersLeft == 0;

            undo.After(this);
            return undo;
        }

        private int RenewArriveTime()
        {
            var minArriveTime = int.MaxValue;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].dir != null)
                {
                    if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue)
                    {
                        players[i].arriveTime = players[i].shiftTime;
                        players[i].arrivePos = NextCoord(players[i].arrivePos, players[i].dir.Value);
                    }
                }

                if (players[i].arriveTime < minArriveTime)
                    minArriveTime = players[i].arriveTime;
            }

            if (minArriveTime == 0)
                minArriveTime = 1;

            return minArriveTime;
        }

        private void Move(int timeDelta)
        {
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                players[i].Move(timeDelta);
            }
        }

        private void MoveDone()
        {
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                players[i].MoveDone();
            }
        }

        private void CheckIsAte()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status != PlayerStatus.Active)
                    continue;

                var prevPosIsEaten = capture.IsEaten(players[i].pos, i);
                if (prevPosIsEaten)
                {
                    if (players[i].arriveTime != 0)
                        players[i].status = PlayerStatus.Loser;
                    else
                    {
                        var isEaten = capture.IsEaten(players[i].arrivePos, i);
                        if (isEaten)
                        {
                            players[i].status = PlayerStatus.Loser;
                        }
                    }
                }
            }
        }

        private void CheckLoss(Config config)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arrivePos == ushort.MaxValue)
                    players[i].status = PlayerStatus.Loser;

                for (int k = 0; k < players.Length; k++)
                {
                    if (players[k].status == PlayerStatus.Eliminated)
                        continue;

                    if (players[k].arriveTime != 0)
                        continue;

                    if (players[k].arrivePos != ushort.MaxValue && (lines[players[k].arrivePos] & (1 << i)) != 0)
                    {
                        players[i].status = PlayerStatus.Loser;
                        if (k != i)
                            players[k].tickScore += Env.LINE_KILL_SCORE;
                    }
                }
            }

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue && capture.territoryCaptureCount[i] == 0)
                    players[i].UpdateLines(i, this);
            }

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

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
                            if ((players[i].lineCount) >= players[k].lineCount)
                            {
                                var collides = false;
                                if (players[i].arrivePos == players[k].arrivePos)
                                    collides = true;
                                else
                                {
                                    if (players[i].arrivePos == players[k].pos && players[k].arriveTime > 0)
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
                                    else if (players[k].arrivePos == players[i].pos && players[i].arriveTime > 0)
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
        }
    }
}