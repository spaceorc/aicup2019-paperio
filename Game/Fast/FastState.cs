using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Protocol;

namespace Game.Fast
{
    public class FastState
    {
        public bool isGameOver;

        public int time;
        public int playersLeft;

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
                    var result = prev + Env.X_CELLS_COUNT;
                    if (result >= Env.CELLS_COUNT)
                        return ushort.MaxValue;
                    return (ushort)result;

                case Direction.Left:
                    if (prev % Env.X_CELLS_COUNT == 0)
                        return ushort.MaxValue;
                    return (ushort)(prev - 1);

                case Direction.Down:
                    result = prev - Env.X_CELLS_COUNT;
                    if (result < 0)
                        return ushort.MaxValue;
                    return (ushort)result;

                case Direction.Right:
                    if (prev % Env.X_CELLS_COUNT == Env.X_CELLS_COUNT - 1)
                        return ushort.MaxValue;
                    return (ushort)(prev + 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        public ushort ToCoord(V v)
        {
            return (ushort)(v.X + v.Y * Env.X_CELLS_COUNT);
        }

        public V ToV(ushort c)
        {
            return V.Get(c % Env.X_CELLS_COUNT, c / Env.X_CELLS_COUNT);
        }

        public string Print(bool territoryOnly = false)
        {
            const string tc = "ABCDEF";
            using (var writer = new StringWriter())
            {
                writer.WriteLine($"score: {string.Join(",", players.Select(x => x.score))}");
                writer.WriteLine($"territory: {string.Join(",", players.Select(x => x.territory))}");
                writer.WriteLine($"nitroLeft: {string.Join(",", players.Select(x => x.nitroLeft))}");
                writer.WriteLine($"slowLeft: {string.Join(",", players.Select(x => x.slowLeft))}");
                writer.WriteLine($"nitrosCollected: {string.Join(",", players.Select(x => x.nitrosCollected))}");
                writer.WriteLine($"slowsCollected: {string.Join(",", players.Select(x => x.slowsCollected))}");
                writer.WriteLine($"opponentTerritoryCaptured: {string.Join(",", players.Select(x => x.opponentTerritoryCaptured))}");
                writer.WriteLine($"lineCount: {string.Join(",", players.Select(x => x.lineCount))}");
                for (int y = Env.Y_CELLS_COUNT - 1; y >= 0; y--)
                {
                    for (int x = 0; x < Env.X_CELLS_COUNT; x++)
                    {
                        var c = (ushort)(y * Env.X_CELLS_COUNT + x);

                        int player = -1;
                        for (int p = 0; p < players.Length; p++)
                        {
                            if (players[p].status != PlayerStatus.Eliminated && (players[p].pos == c || players[p].arrivePos == c))
                            {
                                player = p;
                                break;
                            }
                        }

                        var tomb = false;
                        for (int p = 0; p < players.Length; p++)
                        {
                            if (players[p].status == PlayerStatus.Eliminated && (players[p].pos == c || players[p].arrivePos == c))
                            {
                                tomb = true;
                                break;
                            }
                        }

                        FastBonus bonus = null;
                        for (int b = 0; b < bonusCount; b++)
                        {
                            if (bonuses[b].pos == c)
                            {
                                bonus = bonuses[b];
                                break;
                            }
                        }

                        if (territoryOnly)
                        {
                            if (territory[c] == 0xFF)
                                writer.Write('.');
                            else
                                writer.Write(tc[territory[c]]);
                        }
                        else
                        {
                            if (bonus?.type == BonusType.N)
                                writer.Write('N');
                            else if (bonus?.type == BonusType.S)
                                writer.Write('S');
                            else if (bonus?.type == BonusType.Saw)
                                writer.Write('W');
                            else if (player != -1)
                                writer.Write(player);
                            else if (tomb)
                                writer.Write('*');
                            else if (lines[c] != 0)
                                writer.Write('x');
                            else if (territory[c] == 0xFF)
                                writer.Write('.');
                            else
                                writer.Write(tc[territory[c]]);
                        }
                    }

                    writer.WriteLine();
                }

                return writer.ToString();
            }
        }

        public void SetInput(RequestInput input, string my = "i")
        {
            if (players == null)
            {
                players = new FastPlayer[input.players.Count];

                territory = new byte[Env.CELLS_COUNT];
                lines = new byte[Env.Y_CELLS_COUNT * Env.X_CELLS_COUNT];

                undos = new UndoDataPool(players.Length);
            }

            isGameOver = false;

            capture.Init(players.Length);

            time = input.tick_num;
            playersLeft = input.players.Count;

            for (int c = 0; c < Env.CELLS_COUNT; c++)
            {
                territory[c] = 0xFF;
                lines[c] = 0;
            }

            var keys = new[] {my}.Concat(input.players.Keys.Where(k => k != my)).ToList();
            
            for (var i = 0; i < players.Length; i++)
            {
                var key = i < keys.Count ? keys[i] : "unknown";

                if (players[i] == null)
                {
                    players[i] = new FastPlayer();
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

                    var speed = accel == 0 ? Env.SPEED
                        : accel == -1 ? Env.SLOW_SPEED
                        : accel == 1 ? Env.NITRO_SPEED
                        : throw new InvalidOperationException();

                    var v = playerData.position;
                    var arriveTime = 0;
                    while (!v.InCellCenter(Env.WIDTH))
                    {
                        v += GetShift(playerData.direction ?? throw new InvalidOperationException(), speed);
                        arriveTime++;
                    }

                    players[i].arrivePos = ToCoord(v.ToCellCoords(Env.WIDTH));
                    if (arriveTime == 0)
                        players[i].pos = players[i].arrivePos;
                    else
                        players[i].pos = NextCoord(players[i].arrivePos, (Direction)(((int)(playerData.direction ?? throw new InvalidOperationException()) + 2) % 4));

                    players[i].status = playerData.direction == null && i != 0
                        ? PlayerStatus.Broken
                        : PlayerStatus.Active;

                    players[i].dir = playerData.direction;
                    players[i].score = playerData.score;
                    players[i].shiftTime = accel == 0 ? Env.TICKS_PER_REQUEST
                        : accel == -1 ? Env.SLOW_TICKS_PER_REQUEST
                        : accel == 1 ? Env.NITRO_TICKS_PER_REQUEST
                        : throw new InvalidOperationException();

                    players[i].arriveTime = arriveTime;
                    players[i].slowLeft = playerData.bonuses.FirstOrDefault(b => b.type == BonusType.S)?.ticks ?? 0;
                    players[i].nitroLeft = playerData.bonuses.FirstOrDefault(b => b.type == BonusType.N)?.ticks ?? 0;
                    players[i].lineCount = playerData.lines.Length;
                    players[i].territory = playerData.territory.Length;
                    players[i].slowsCollected = 0;
                    players[i].nitrosCollected = 0;
                    players[i].killedBy = 0;
                    players[i].opponentTerritoryCaptured = 0;

                    for (var k = 0; k < playerData.lines.Length; k++)
                    {
                        var lv = ToCoord(playerData.lines[k].ToCellCoords(Env.WIDTH));
                        players[i].line[k] = lv;
                        lines[lv] = (byte)(lines[lv] | (1 << i));
                    }

                    for (var k = 0; k < playerData.territory.Length; k++)
                    {
                        var lv = ToCoord(playerData.territory[k].ToCellCoords(Env.WIDTH));
                        territory[lv] = (byte)i;
                    }
                }
            }

            if (bonuses == null || bonuses.Length < input.bonuses.Length)
                bonuses = new FastBonus[input.bonuses.Length];

            bonusCount = 0;
            for (var i = 0; i < input.bonuses.Length; i++)
            {
                bonuses[bonusCount++] = new FastBonus(this, input.bonuses[i]);
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

        public UndoData NextTurn(Direction[] commands, bool withUndo, int commandsStart = 0)
        {
            UndoData undo = null;
            if (withUndo)
            {
                undo = undos.Get();
                undo.Before(this);
            }

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated || players[i].status == PlayerStatus.Broken)
                    continue;

                if (players[i].arriveTime == 0)
                {
#if DEBUG
                    if (players[i].dir != null)
                    {
                        if ((Direction)(((int)players[i].dir.Value + 2) % 4) == commands[commandsStart + i])
                            throw new InvalidOperationException($"Bad command: {commands[commandsStart + i]}");
                    }
#endif

                    players[i].dir = commands[commandsStart + i];
                }
            }

            var timeDelta = RenewArriveTime();

            time += timeDelta;
            if (time > Env.MAX_TICK_COUNT)
            {
                isGameOver = true;
                if (withUndo)
                    undo.After(this);

                return undo;
            }

            Move(timeDelta);

            CheckIntermediateCollisions();

            // Main
            capture.Clear();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated || players[i].status == PlayerStatus.Broken)
                    continue;

                if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue)
                {
                    // Capture
                    capture.Capture(this, i);
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

            CheckLoss();

            CheckIsAte();

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated || players[i].status == PlayerStatus.Broken)
                    continue;

                if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue)
                {
                    players[i].TickAction();

                    for (int b = 0; b < bonusCount;)
                    {
                        if (bonuses[b].pos != players[i].arrivePos && !capture.BelongsTo(bonuses[b].pos, i))
                            b++;
                        else
                        {
                            if (bonuses[b].type == BonusType.N)
                            {
                                var bonusTime = i == 0 ? 10 : 50;
                                if (players[i].nitroLeft > 0)
                                    players[i].nitroLeft += bonusTime;
                                else
                                    players[i].nitroLeft = bonusTime;
                                players[i].UpdateShiftTime();
                                players[i].nitrosCollected++;
                            }
                            else if (bonuses[b].type == BonusType.S)
                            {
                                var bonusTime = i == 0 ? 50 : 10;
                                if (players[i].slowLeft > 0)
                                    players[i].slowLeft += bonusTime; // random 10..50
                                else
                                    players[i].slowLeft = bonusTime;
                                players[i].UpdateShiftTime();
                                players[i].slowsCollected++;
                            }
                            else if (bonuses[b].type == BonusType.Saw)
                            {
                                var sawStatus = 0ul;
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
                                            sawStatus |= 0xFFul << (k * 8);
                                            players[k].status = PlayerStatus.Loser;
                                            players[k].killedBy = (byte)(players[k].killedBy | (1 << i));
                                            players[i].tickScore += Env.SAW_KILL_SCORE;
                                        }
                                    }

                                    var owner = territory[v];
                                    if (owner != 0xFF && owner != i)
                                    {
                                        if (players[owner].status != PlayerStatus.Eliminated)
                                            sawStatus |= 1ul << (owner * 8);
                                    }
                                }

                                for (int k = 0; k < players.Length; k++)
                                {
                                    if (k == i || players[k].status == PlayerStatus.Eliminated)
                                        continue;

                                    if (((sawStatus >> (k * 8)) & 0xFF) != 1)
                                        continue;

                                    players[i].tickScore += Env.SAW_SCORE;
                                    var vx = v % Env.X_CELLS_COUNT;
                                    var vy = v / Env.X_CELLS_COUNT;
                                    if (players[i].dir.Value == Direction.Up || players[i].dir.Value == Direction.Down)
                                    {
                                        if (players[k].arrivePos % Env.X_CELLS_COUNT < vx)
                                        {
                                            int pos = 0;
                                            for (int y = 0; y < Env.Y_CELLS_COUNT; y++)
                                            {
                                                pos += vx;
                                                for (int x = vx; x < Env.X_CELLS_COUNT; x++, pos++)
                                                {
                                                    if (territory[pos] == k)
                                                    {
                                                        players[k].territory--;
                                                        undo?.NotifyCapture(this);
                                                        territory[pos] = 0xFF;
                                                        territoryVersion++;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int pos = 0;
                                            for (int y = 0; y < Env.Y_CELLS_COUNT; y++)
                                            {
                                                for (int x = 0; x <= vx; x++, pos++)
                                                {
                                                    if (territory[pos] == k)
                                                    {
                                                        players[k].territory--;
                                                        undo?.NotifyCapture(this);
                                                        territory[pos] = 0xFF;
                                                        territoryVersion++;
                                                    }
                                                }

                                                pos += Env.X_CELLS_COUNT - vx - 1;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (players[k].arrivePos / Env.X_CELLS_COUNT < vy)
                                        {
                                            int pos = vy * Env.X_CELLS_COUNT;
                                            for (int y = vy; y < Env.Y_CELLS_COUNT; y++)
                                            for (int x = 0; x < Env.X_CELLS_COUNT; x++, pos++)
                                            {
                                                if (territory[pos] == k)
                                                {
                                                    players[k].territory--;
                                                    undo?.NotifyCapture(this);
                                                    territory[pos] = 0xFF;
                                                    territoryVersion++;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int pos = 0;
                                            for (int y = 0; y <= vy; y++)
                                            for (int x = 0; x < Env.X_CELLS_COUNT; x++, pos++)
                                            {
                                                if (territory[pos] == k)
                                                {
                                                    players[k].territory--;
                                                    undo?.NotifyCapture(this);
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
                }
            }

            capture.ApplyTo(this, undo);

            MoveDone();

            playersLeft = 0;
            for (int i = 0; i < players.Length; i++)
            {
                switch (players[i].status)
                {
                    case PlayerStatus.Loser:
                        players[i].status = PlayerStatus.Eliminated;
                        break;
                    case PlayerStatus.Active:
                    case PlayerStatus.Broken:
                        playersLeft++;
                        players[i].score += players[i].tickScore;
                        break;
                }

                players[i].tickScore = 0;
            }

            isGameOver = playersLeft == 0;

            if (withUndo)
                undo.After(this);
            return undo;
        }

        private int RenewArriveTime()
        {
            var minArriveTime = int.MaxValue;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated || players[i].status == PlayerStatus.Broken)
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
                if (players[i].status == PlayerStatus.Eliminated || players[i].status == PlayerStatus.Loser)
                    continue;

                var prevPosEatenBy = capture.EatenBy(players[i].pos, i);
                if (prevPosEatenBy != 0)
                {
                    if (players[i].arriveTime != 0)
                    {
                        players[i].status = PlayerStatus.Loser;
                        players[i].killedBy = (byte)(players[i].killedBy | prevPosEatenBy);
                    }
                    else
                    {
                        var eatenBy = capture.EatenBy(players[i].arrivePos, i);
                        if ((eatenBy & prevPosEatenBy) != 0)
                        {
                            players[i].status = PlayerStatus.Loser;
                            players[i].killedBy = (byte)(players[i].killedBy | (eatenBy & prevPosEatenBy));
                        }
                    }
                }
            }
        }

        private void CheckLoss()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                if (players[i].status != PlayerStatus.Loser)
                {
                    if (players[i].territory == 0)
                        players[i].status = PlayerStatus.Loser;
                    else if (players[i].arrivePos == ushort.MaxValue)
                        players[i].status = PlayerStatus.Loser;
                    else if (players[i].arriveTime == 0 && (lines[players[i].arrivePos] & (1 << i)) != 0)
                        players[i].status = PlayerStatus.Loser;
                }

                for (int k = i + 1; k < players.Length; k++)
                {
                    if (players[k].status == PlayerStatus.Eliminated)
                        continue;

                    if (players[k].arriveTime == 0 && players[k].arrivePos != ushort.MaxValue && (lines[players[k].arrivePos] & (1 << i)) != 0)
                    {
                        players[i].status = PlayerStatus.Loser;
                        players[i].killedBy = (byte)(players[i].killedBy | (1 << k));
                        players[k].tickScore += Env.LINE_KILL_SCORE;
                    }

                    if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue && (lines[players[i].arrivePos] & (1 << k)) != 0)
                    {
                        players[k].status = PlayerStatus.Loser;
                        players[k].killedBy = (byte)(players[k].killedBy | (1 << i));
                        players[i].tickScore += Env.LINE_KILL_SCORE;
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

            for (int i = 0; i < players.Length - 1; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                for (int k = i + 1; k < players.Length; k++)
                {
                    if (players[k].status == PlayerStatus.Eliminated)
                        continue;

                    if ((players[i].status == PlayerStatus.Loser || players[i].lineCount < players[k].lineCount)
                        && (players[k].status == PlayerStatus.Loser || players[k].lineCount < players[i].lineCount))
                        continue;

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
                                var distArrive1 = players[i].arriveTime * players[k].shiftTime;
                                var distArrive2 = players[k].arriveTime * players[i].shiftTime;
                                collides = distArrive1 < distArrive2;
                            }
                        }
                        else if (players[k].arrivePos == players[i].pos && players[i].arriveTime > 0)
                        {
                            if (players[k].dir != players[i].dir)
                                collides = true;
                            else
                            {
                                var distArrive1 = players[i].arriveTime * players[k].shiftTime;
                                var distArrive2 = players[k].arriveTime * players[i].shiftTime;
                                collides = distArrive2 < distArrive1;
                            }
                        }
                    }

                    if (collides)
                    {
                        if (players[i].status != PlayerStatus.Loser && players[i].lineCount >= players[k].lineCount)
                        {
                            players[i].status = PlayerStatus.Loser;
                            players[i].killedBy = (byte)(players[i].killedBy | (1 << k));
                        }

                        if (players[k].status != PlayerStatus.Loser && players[k].lineCount >= players[i].lineCount)
                        {
                            players[k].status = PlayerStatus.Loser;
                            players[k].killedBy = (byte)(players[k].killedBy | (1 << i));
                        }
                    }
                }
            }
        }

        private void CheckIntermediateCollisions()
        {
            for (int i = 0; i < players.Length - 1; i++)
            {
                if (players[i].status == PlayerStatus.Eliminated)
                    continue;

                for (int k = i + 1; k < players.Length; k++)
                {
                    if (players[k].status == PlayerStatus.Eliminated)
                        continue;

                    var collides = false;
                    if (players[i].arrivePos == players[k].arrivePos)
                    {
                        if (players[i].arriveTime == 0)
                        {
                            if (players[k].arriveTime == 0)
                                collides = true;
                            else if (players[k].shiftTime - players[k].arriveTime > 1)
                                collides = true;
                        }
                        else if (players[k].arriveTime == 0)
                        {
                            if (players[i].shiftTime - players[i].arriveTime > 1)
                                collides = true;
                        }
                    }
                    else
                    {
                        if (players[i].arrivePos == players[k].pos)
                        {
                            if (players[k].arrivePos == players[i].pos)
                                collides = true;
                            else if (players[k].arriveTime > 0 || players[i].shiftTime - players[i].arriveTime > 1)
                            {
                                if (players[i].dir != players[k].dir)
                                    collides = true;
                                else
                                {
                                    var distArrive1 = players[i].arriveTime * players[k].shiftTime;
                                    var distArrive2 = players[k].arriveTime * players[i].shiftTime;
                                    collides = distArrive1 < distArrive2;
                                }
                            }
                        }
                        else if (players[k].arrivePos == players[i].pos)
                        {
                            if (players[i].arriveTime > 0 || players[k].shiftTime - players[k].arriveTime > 1)
                            {
                                if (players[i].dir != players[k].dir)
                                    collides = true;
                                else
                                {
                                    var distArrive1 = players[i].arriveTime * players[k].shiftTime;
                                    var distArrive2 = players[k].arriveTime * players[i].shiftTime;
                                    collides = distArrive2 < distArrive1;
                                }
                            }
                        }
                    }

                    if (collides)
                    {
                        if (players[i].lineCount >= players[k].lineCount)
                        {
                            players[i].status = PlayerStatus.Eliminated;
                            players[i].killedBy = (byte)(players[i].killedBy | (1 << k));
                        }

                        if (players[k].lineCount >= players[i].lineCount)
                        {
                            players[k].status = PlayerStatus.Eliminated;
                            players[k].killedBy = (byte)(players[k].killedBy | (1 << i));
                        }
                    }
                }
            }
        }

        public Direction MakeDir(ushort prev, ushort next)
        {
            var diff = next - prev;
            if (diff == 1)
                return Direction.Right;
            if (diff == -1)
                return Direction.Left;
            if (diff == Env.X_CELLS_COUNT)
                return Direction.Up;
            if (diff == -Env.X_CELLS_COUNT)
                return Direction.Down;
            throw new InvalidOperationException($"Bad cell diff: {diff}");
        }

        public int MDist(ushort a, ushort b)
        {
            var ax = a % Env.X_CELLS_COUNT;
            var ay = a / Env.X_CELLS_COUNT;
            var bx = b % Env.X_CELLS_COUNT;
            var by = b / Env.X_CELLS_COUNT;
            return Math.Abs(ax - bx) + Math.Abs(ay - by);
        }
    }
}