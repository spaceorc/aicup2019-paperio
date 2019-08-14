using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;
using Game.Types;

namespace Game.Unsafe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct UnsafeState
    {
        public const int Size = 1 + 2 + 6 * UnsafePlayer.Size + 961;

        static UnsafeState()
        {
            if (sizeof(UnsafeState) != Size)
                throw new InvalidOperationException($"sizeof(UnsafeState) == {sizeof(UnsafeState)} with expected {Size}");
        }

        public byte playersCount; // 1
        public ushort time; // 2

        // owner (0..6, 7 nobody) - 3 bit
        // line (0..6, 7 nobody) - 3 bit
        // bonus (01 - nitro, 10 - slow, 11 - saw, 00 - nothing) - 2 bit
        public const byte TERRITORY_OWNER_MASK = 0b00000111;
        public const byte TERRITORY_OWNER_NO = 0b00000111;

        public const byte TERRITORY_LINE_MASK = 0b00111000;
        public const byte TERRITORY_LINE_NO = 0b00111000;
        public const byte TERRITORY_LINE_SHIFT = 3;

        public const byte TERRITORY_BONUS_MASK = 0b11000000;
        public const byte TERRITORY_BONUS_NO = 0b00000000;
        public const byte TERRITORY_BONUS_NITRO = 0b01000000;
        public const byte TERRITORY_BONUS_SLOW = 0b10000000;
        public const byte TERRITORY_BONUS_SAW = 0b11000000;

        public fixed byte players[6 * UnsafePlayer.Size]; // 6*17 = 102
        public fixed byte territory[Env.CELLS_COUNT]; // 961

        public string Print(bool territoryOnly = false)
        {
            fixed (UnsafeState* that = &this)
            {
                const string tc = "ABCDEF";
                using (var writer = new StringWriter())
                {
                    var players = (UnsafePlayer*)that->players;
                    var pl = new List<UnsafePlayer>();
                    for (int p = 0; p < that->playersCount; p++)
                        pl.Add(players[p]);

                    writer.WriteLine($"score: {string.Join(",", pl.Select(x => x.score))}");
                    writer.WriteLine($"territory: {string.Join(",", pl.Select(x => x.territory))}");
                    writer.WriteLine($"nitroLeft: {string.Join(",", pl.Select(x => x.nitroLeft))}");
                    writer.WriteLine($"slowLeft: {string.Join(",", pl.Select(x => x.slowLeft))}");
                    writer.WriteLine($"nitrosCollected: {string.Join(",", pl.Select(x => x.nitrosCollected))}");
                    writer.WriteLine($"slowsCollected: {string.Join(",", pl.Select(x => x.slowsCollected))}");
                    writer.WriteLine($"opponentTerritoryCaptured: {string.Join(",", pl.Select(x => x.opponentTerritoryCaptured))}");
                    writer.WriteLine($"lineCount: {string.Join(",", pl.Select(x => x.lineCount))}");
                    for (int y = Env.Y_CELLS_COUNT - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < Env.X_CELLS_COUNT; x++)
                        {
                            var c = (ushort)(y * Env.X_CELLS_COUNT + x);

                            int player = -1;
                            for (int p = 0; p < that->playersCount; p++)
                            {
                                if (players[p].status != UnsafePlayer.STATUS_ELIMINATED && (players[p].pos == c || players[p].arrivePos == c))
                                {
                                    player = p;
                                    break;
                                }
                            }

                            var tomb = false;
                            for (int p = 0; p < that->playersCount; p++)
                            {
                                if (players[p].status == UnsafePlayer.STATUS_ELIMINATED && (players[p].pos == c || players[p].arrivePos == c))
                                {
                                    tomb = true;
                                    break;
                                }
                            }

                            var bonus = (that->territory[c] & TERRITORY_BONUS_MASK) == TERRITORY_BONUS_NITRO ? 'N'
                                : (that->territory[c] & TERRITORY_BONUS_MASK) == TERRITORY_BONUS_SLOW ? 'S'
                                : (that->territory[c] & TERRITORY_BONUS_MASK) == TERRITORY_BONUS_SAW ? 'W'
                                : (char?)null;

                            if (territoryOnly)
                            {
                                if ((that->territory[c] & TERRITORY_OWNER_MASK) == TERRITORY_OWNER_NO)
                                    writer.Write('.');
                                else
                                    writer.Write(tc[that->territory[c] & TERRITORY_OWNER_MASK]);
                            }
                            else
                            {
                                if (bonus != null)
                                    writer.Write(bonus.Value);
                                else if (player != -1)
                                    writer.Write(player);
                                else if (tomb)
                                    writer.Write('*');
                                else if ((that->territory[c] & TERRITORY_LINE_MASK) != TERRITORY_LINE_NO)
                                    writer.Write('x');
                                else if ((that->territory[c] & TERRITORY_OWNER_MASK) == TERRITORY_OWNER_NO)
                                    writer.Write('.');
                                else
                                    writer.Write(tc[that->territory[c] & TERRITORY_OWNER_MASK]);
                            }
                        }

                        writer.WriteLine();
                    }

                    return writer.ToString();
                }
            }
        }

        public void SetInput(RequestInput input, string my = "i")
        {
            fixed (UnsafeState* that = &this)
            {
                that->playersCount = (byte)input.players.Count;
                that->time = (ushort)input.tick_num;
                for (int i = 0; i < Env.CELLS_COUNT; i++)
                    that->territory[i] = TERRITORY_BONUS_NO | TERRITORY_LINE_NO | TERRITORY_OWNER_NO;

                var pi = 0;
                AddPlayer(that, pi++, input.players[my]);
                foreach (var kvp in input.players.Where(x => x.Key != my))
                    AddPlayer(that, pi++, kvp.Value);

                for (var i = 0; i < input.bonuses.Length; i++)
                {
                    var lv = ToCoord(input.bonuses[i].position.ToCellCoords(Env.WIDTH));
                    that->territory[lv] = (byte)(that->territory[lv] & ~TERRITORY_BONUS_MASK
                                                 | (input.bonuses[i].type == BonusType.N ? TERRITORY_BONUS_NITRO
                                                     : input.bonuses[i].type == BonusType.S ? TERRITORY_BONUS_SLOW
                                                     : input.bonuses[i].type == BonusType.Saw ? TERRITORY_BONUS_SAW
                                                     : throw new InvalidOperationException()));
                }
            }
        }

        private static void AddPlayer(UnsafeState* that, int index, RequestInput.PlayerData playerData)
        {
            var player = (UnsafePlayer*)that->players + index;
            player->dir = playerData.direction == null ? UnsafePlayer.DIR_NULL : (byte)playerData.direction.Value;
            player->score = (ushort)playerData.score;
            player->slowLeft = (byte)(playerData.bonuses.FirstOrDefault(b => b.type == BonusType.S)?.ticks ?? 0);
            player->nitroLeft = (byte)(playerData.bonuses.FirstOrDefault(b => b.type == BonusType.N)?.ticks ?? 0);
            player->lineCount = (ushort)playerData.lines.Length;
            player->territory = (ushort)playerData.territory.Length;
            player->slowsCollected = 0;
            player->nitrosCollected = 0;
            player->killedBy = 0;
            player->opponentTerritoryCaptured = 0;
            player->UpdateShiftTime();

            var accel = 0;
            if (player->nitroLeft > 0)
                accel++;
            if (player->slowLeft > 0)
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

            player->arrivePos = ToCoord(v.ToCellCoords(Env.WIDTH));
            player->arriveTime = (byte)arriveTime;
            if (arriveTime == 0)
                player->pos = player->arrivePos;
            else
                player->pos = NextCoord(player->arrivePos, (byte)((player->dir + 2) % 4));

            player->status = playerData.direction == null && index != 0
                ? UnsafePlayer.STATUS_BROKEN
                : UnsafePlayer.STATUS_ACTIVE;

            for (var k = 0; k < playerData.lines.Length; k++)
            {
                var lv = ToCoord(playerData.lines[k].ToCellCoords(Env.WIDTH));
                that->territory[lv] = (byte)(that->territory[lv] & ~TERRITORY_LINE_MASK | (index << TERRITORY_LINE_SHIFT));
            }

            for (var k = 0; k < playerData.territory.Length; k++)
            {
                var lv = ToCoord(playerData.territory[k].ToCellCoords(Env.WIDTH));
                that->territory[lv] = (byte)(that->territory[lv] & ~TERRITORY_OWNER_MASK | index);
            }

            V GetShift(Direction direction, int d) =>
                direction == Direction.Up ? V.Get(0, d)
                : direction == Direction.Down ? V.Get(0, -d)
                : direction == Direction.Right ? V.Get(d, 0)
                : direction == Direction.Left ? V.Get(-d, 0)
                : throw new InvalidOperationException($"Unknown direction: {direction}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyCommand(int player, byte dir)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                p[player].dir = dir;
            }
        }

        public void NextTurn(UnsafeCapture* capture, UnsafeUndo* undo)
        {
            fixed (UnsafeState* that = &this)
            {
                var timeDelta = RenewArriveTime();

                if (undo != null)
                    undo->AfterCommands(that);

                that->time = (ushort)(that->time + timeDelta);
                if (that->time > Env.MAX_TICK_COUNT)
                    return;

                Move(timeDelta);

                var tickScores = stackalloc ushort[that->playersCount];
                for (int i = 0; i < that->playersCount; i++)
                    tickScores[i] = 0;

                CheckIntermediateCollisions();

                Capture(capture, tickScores);

                CheckLoss(tickScores, capture);

                CheckIsAte(capture);

                CaptureBonuses(tickScores, capture, undo);

                capture->ApplyTo(that, tickScores, undo);

                MoveDone();

                Done(tickScores);
            }
        }

        private byte RenewArriveTime()
        {
            fixed (UnsafeState* that = &this)
            {
                var minArriveTime = byte.MaxValue;
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status != UnsafePlayer.STATUS_ACTIVE)
                        continue;

                    if (p->dir != UnsafePlayer.DIR_NULL)
                    {
                        if (p->arriveTime == 0 && p->arrivePos != ushort.MaxValue)
                        {
                            p->arriveTime = p->shiftTime;
                            p->arrivePos = NextCoord(p->arrivePos, p->dir);
                        }
                    }

                    if (p->arriveTime < minArriveTime)
                        minArriveTime = p->arriveTime;
                }

                if (minArriveTime == 0)
                    minArriveTime = 1;

                return minArriveTime;
            }
        }

        private void Done(ushort* tickScores)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                var playersLeft = 0;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    switch (p->status)
                    {
                        case UnsafePlayer.STATUS_LOOSER:
                            p->status = UnsafePlayer.STATUS_ELIMINATED;
                            break;
                        case UnsafePlayer.STATUS_ACTIVE:
                        case UnsafePlayer.STATUS_BROKEN:
                            p->score += tickScores[i];
                            playersLeft++;
                            break;
                    }
                }

                if (playersLeft == 0)
                    that->time = ushort.MaxValue;
            }
        }

        private void CheckIntermediateCollisions()
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount - 1; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    var pk = p + 1;
                    for (int k = i + 1; k < that->playersCount; k++, pk++)
                    {
                        if (pk->status == UnsafePlayer.STATUS_ELIMINATED)
                            continue;

                        var collides = false;
                        if (p->arrivePos == pk->arrivePos)
                        {
                            if (p->arriveTime == 0)
                            {
                                if (pk->arriveTime == 0)
                                    collides = true;
                                else if (pk->shiftTime - pk->arriveTime > 1)
                                    collides = true;
                            }
                            else if (pk->arriveTime == 0)
                            {
                                if (p->shiftTime - p->arriveTime > 1)
                                    collides = true;
                            }
                        }
                        else
                        {
                            if (p->arrivePos == pk->pos)
                            {
                                if (pk->arrivePos == p->pos)
                                    collides = true;
                                else if (pk->arriveTime > 0 || p->shiftTime - p->arriveTime > 1)
                                {
                                    if (p->dir != pk->dir)
                                        collides = true;
                                    else
                                    {
                                        var distArrive1 = p->arriveTime * pk->shiftTime;
                                        var distArrive2 = pk->arriveTime * p->shiftTime;
                                        collides = distArrive1 < distArrive2;
                                    }
                                }
                            }
                            else if (pk->arrivePos == p->pos)
                            {
                                if (p->arriveTime > 0 || pk->shiftTime - pk->arriveTime > 1)
                                {
                                    if (p->dir != pk->dir)
                                        collides = true;
                                    else
                                    {
                                        var distArrive1 = p->arriveTime * pk->shiftTime;
                                        var distArrive2 = pk->arriveTime * p->shiftTime;
                                        collides = distArrive2 < distArrive1;
                                    }
                                }
                            }
                        }

                        if (collides)
                        {
                            if (p->lineCount >= pk->lineCount)
                            {
                                p->status = UnsafePlayer.STATUS_ELIMINATED;
                                p->killedBy = (byte)(p->killedBy | (1 << k));
                            }

                            if (pk->lineCount >= p->lineCount)
                            {
                                pk->status = UnsafePlayer.STATUS_ELIMINATED;
                                pk->killedBy = (byte)(pk->killedBy | (1 << i));
                            }
                        }
                    }
                }
            }
        }

        private void CheckIsAte(UnsafeCapture* capture)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED || p->status == UnsafePlayer.STATUS_LOOSER)
                        continue;

                    var prevPosEatenBy = capture->EatenBy(p->pos, i);
                    if (prevPosEatenBy != 0)
                    {
                        if (p->arriveTime != 0)
                        {
                            p->status = UnsafePlayer.STATUS_LOOSER;
                            p->killedBy = (byte)(p->killedBy | prevPosEatenBy);
                        }
                        else
                        {
                            var eatenBy = capture->EatenBy(p->arrivePos, i);
                            if ((eatenBy & prevPosEatenBy) != 0)
                            {
                                p->status = UnsafePlayer.STATUS_LOOSER;
                                p->killedBy = (byte)(p->killedBy | (eatenBy & prevPosEatenBy));
                            }
                        }
                    }
                }
            }
        }

        private void CaptureBonuses(ushort* tickScores, UnsafeCapture* capture, UnsafeUndo* undo)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED || p->status == UnsafePlayer.STATUS_BROKEN)
                        continue;

                    if (p->arriveTime == 0 && p->arrivePos != ushort.MaxValue)
                    {
                        p->TickAction();

                        for (int b = 0; b < capture->capturedBonusesCount; b++)
                        {
                            var bonusPos = capture->capturedBonusesAt[b];
                            if (bonusPos == p->arrivePos || capture->BelongsTo(bonusPos, i))
                            {
                                var bonus = (byte)(that->territory[bonusPos] & TERRITORY_BONUS_MASK);
                                if (bonus == TERRITORY_BONUS_NITRO)
                                {
                                    var bonusTime = i == 0 ? 10 : 50;
                                    if (p->nitroLeft > 0)
                                        p->nitroLeft = (byte)(p->nitroLeft + bonusTime);
                                    else
                                        p->nitroLeft = (byte)bonusTime;
                                    p->UpdateShiftTime();
                                    p->nitrosCollected++;
                                }
                                else if (bonus == TERRITORY_BONUS_SLOW)
                                {
                                    var bonusTime = i == 0 ? 50 : 10;
                                    if (p->slowLeft > 0)
                                        p->slowLeft = (byte)(p->slowLeft + bonusTime); // random 10..50
                                    else
                                        p->slowLeft = (byte)bonusTime;
                                    p->UpdateShiftTime();
                                    p->slowsCollected++;
                                }
                                else if (bonus == TERRITORY_BONUS_SAW)
                                {
                                    var sawStatus = 0ul;
                                    var v = p->arrivePos;
                                    while (true)
                                    {
                                        v = NextCoord(v, p->dir);
                                        if (v == ushort.MaxValue)
                                            break;
                                        var ppk = (UnsafePlayer*)that->players;
                                        for (int k = 0; k < that->playersCount; k++, ppk++)
                                        {
                                            if (k == i || ppk->status == UnsafePlayer.STATUS_ELIMINATED)
                                                continue;

                                            if (ppk->arrivePos == v || (ppk->pos == v && ppk->arriveTime > 0))
                                            {
                                                sawStatus |= 0xFFul << (k * 8);
                                                ppk->status = UnsafePlayer.STATUS_LOOSER;
                                                ppk->killedBy = (byte)(ppk->killedBy | (1 << i));
                                                tickScores[i] += Env.SAW_KILL_SCORE;
                                            }
                                        }

                                        var owner = that->territory[v] & TERRITORY_OWNER_MASK;
                                        if (owner != TERRITORY_OWNER_NO && owner != i)
                                        {
                                            var po = (UnsafePlayer*)that->players + owner;
                                            if (po->status != UnsafePlayer.STATUS_ELIMINATED)
                                                sawStatus |= 1ul << (owner * 8);
                                        }
                                    }

                                    var pk = (UnsafePlayer*)that->players;
                                    for (int k = 0; k < that->playersCount; k++, pk++)
                                    {
                                        if (k == i || pk->status == UnsafePlayer.STATUS_ELIMINATED)
                                            continue;

                                        if (((sawStatus >> (k * 8)) & 0xFF) != 1)
                                            continue;

                                        tickScores[i] += Env.SAW_SCORE;
                                        var vx = v % Env.X_CELLS_COUNT;
                                        var vy = v / Env.X_CELLS_COUNT;
                                        if (p->dir == UnsafePlayer.DIR_UP || p->dir == UnsafePlayer.DIR_DOWN)
                                        {
                                            if (pk->arrivePos % Env.X_CELLS_COUNT < vx)
                                            {
                                                int pos = 0;
                                                for (int y = 0; y < Env.Y_CELLS_COUNT; y++)
                                                {
                                                    pos += vx;
                                                    for (int x = vx; x < Env.X_CELLS_COUNT; x++, pos++)
                                                    {
                                                        if ((that->territory[pos] & TERRITORY_OWNER_MASK) == k)
                                                        {
                                                            pk->territory--;
                                                            if (undo != null)
                                                                undo->BeforeTerritoryChange(that);
                                                            that->territory[pos] |= TERRITORY_OWNER_NO;
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
                                                        if ((that->territory[pos] & TERRITORY_OWNER_MASK) == k)
                                                        {
                                                            pk->territory--;
                                                            if (undo != null)
                                                                undo->BeforeTerritoryChange(that);
                                                            that->territory[pos] |= TERRITORY_OWNER_NO;
                                                        }
                                                    }

                                                    pos += Env.X_CELLS_COUNT - vx - 1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (pk->arrivePos / Env.X_CELLS_COUNT < vy)
                                            {
                                                int pos = vy * Env.X_CELLS_COUNT;
                                                for (int y = vy; y < Env.Y_CELLS_COUNT; y++)
                                                for (int x = 0; x < Env.X_CELLS_COUNT; x++, pos++)
                                                {
                                                    if ((that->territory[pos] & TERRITORY_OWNER_MASK) == k)
                                                    {
                                                        pk->territory--;
                                                        if (undo != null)
                                                            undo->BeforeTerritoryChange(that);
                                                        that->territory[pos] |= TERRITORY_OWNER_NO;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                int pos = 0;
                                                for (int y = 0; y <= vy; y++)
                                                for (int x = 0; x < Env.X_CELLS_COUNT; x++, pos++)
                                                {
                                                    if ((that->territory[pos] & TERRITORY_OWNER_MASK) == k)
                                                    {
                                                        pk->territory--;
                                                        if (undo != null)
                                                            undo->BeforeTerritoryChange(that);
                                                        that->territory[pos] |= TERRITORY_OWNER_NO;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (undo != null)
                                    undo->BeforeTerritoryLocalChange(that, bonusPos);
                                that->territory[bonusPos] = (byte)(that->territory[bonusPos] & ~TERRITORY_BONUS_MASK);
                            }
                        }
                    }
                }
            }
        }

        private void CheckLoss(ushort* tickScores, UnsafeCapture* capture)
        {
            fixed (UnsafeState* that = &this)
            {
                var players = (UnsafePlayer*)that->players;
                var p = players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (p->status != UnsafePlayer.STATUS_LOOSER)
                    {
                        if (p->territory == 0)
                            p->status = UnsafePlayer.STATUS_LOOSER;
                        else if (p->arrivePos == ushort.MaxValue)
                            p->status = UnsafePlayer.STATUS_LOOSER;
                    }

                    if (p->arriveTime == 0)
                    {
                        var line = that->territory[p->arrivePos] & TERRITORY_LINE_MASK;
                        if (line != TERRITORY_LINE_NO)
                        {
                            var lineOwner = line >> TERRITORY_LINE_SHIFT;
                            var pk = players + lineOwner;
                            if (lineOwner == i)
                                p->status = UnsafePlayer.STATUS_LOOSER;
                            else if (pk->status != UnsafePlayer.STATUS_ELIMINATED)
                            {
                                pk->status = UnsafePlayer.STATUS_LOOSER;
                                pk->killedBy = (byte)(pk->killedBy | (1 << i));
                                tickScores[i] = (ushort)(tickScores[i] + Env.LINE_KILL_SCORE);
                            }
                        }
                    }
                }

                p = players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (p->arriveTime == 0 && p->arrivePos != ushort.MaxValue && capture->captureCount[i] == 0)
                    {
                        if (p->lineCount > 0 || (that->territory[p->arrivePos] & TERRITORY_OWNER_MASK) != i)
                        {
                            if ((that->territory[p->arrivePos] & TERRITORY_LINE_MASK) != i << TERRITORY_LINE_SHIFT)
                            {
                                that->territory[p->arrivePos] = (byte)(that->territory[p->arrivePos] & ~TERRITORY_LINE_MASK | (i << TERRITORY_LINE_SHIFT));
                                p->lineCount++;
                            }
                        }
                    }
                }

                p = players;
                for (int i = 0; i < that->playersCount - 1; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    var pk = p + 1;
                    for (int k = i + 1; k < that->playersCount; k++, pk++)
                    {
                        if (pk->status == UnsafePlayer.STATUS_ELIMINATED)
                            continue;

                        if ((p->status == UnsafePlayer.STATUS_LOOSER || p->lineCount < pk->lineCount)
                            && (pk->status == UnsafePlayer.STATUS_LOOSER || pk->lineCount < p->lineCount))
                            continue;

                        var collides = false;
                        if (p->arrivePos == pk->arrivePos)
                            collides = true;
                        else
                        {
                            if (p->arrivePos == pk->pos && pk->arriveTime > 0)
                            {
                                if (p->dir != pk->dir)
                                    collides = true;
                                else
                                {
                                    var distArrive1 = p->arriveTime * pk->shiftTime;
                                    var distArrive2 = pk->arriveTime * p->shiftTime;
                                    collides = distArrive1 < distArrive2;
                                }
                            }
                            else if (pk->arrivePos == p->pos && p->arriveTime > 0)
                            {
                                if (pk->dir != p->dir)
                                    collides = true;
                                else
                                {
                                    var distArrive1 = p->arriveTime * pk->shiftTime;
                                    var distArrive2 = pk->arriveTime * p->shiftTime;
                                    collides = distArrive2 < distArrive1;
                                }
                            }
                        }

                        if (collides)
                        {
                            if (p->status != UnsafePlayer.STATUS_LOOSER && p->lineCount >= pk->lineCount)
                            {
                                p->status = UnsafePlayer.STATUS_LOOSER;
                                p->killedBy = (byte)(p->killedBy | (1 << k));
                            }

                            if (pk->status != UnsafePlayer.STATUS_LOOSER && pk->lineCount >= p->lineCount)
                            {
                                pk->status = UnsafePlayer.STATUS_LOOSER;
                                pk->killedBy = (byte)(pk->killedBy | (1 << i));
                            }
                        }
                    }
                }
            }
        }

        private void Capture(UnsafeCapture* capture, ushort* tickScores)
        {
            fixed (UnsafeState* that = &this)
            {
                capture->Clear(that);
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status != UnsafePlayer.STATUS_ACTIVE)
                        continue;

                    if (p->arriveTime == 0 && p->arrivePos != ushort.MaxValue)
                    {
                        // Capture
                        capture->Capture(that, i);
                        tickScores[i] = (ushort)(tickScores[i] + Env.NEUTRAL_TERRITORY_SCORE * capture->captureCount[i]);
                    }
                }
            }
        }

        private void Move(byte timeDelta)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (p->dir == UnsafePlayer.DIR_NULL)
                        continue;

                    p->arriveTime = (byte)(p->arriveTime - timeDelta);
                }
            }
        }

        private void MoveDone()
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < that->playersCount; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (p->dir == UnsafePlayer.DIR_NULL)
                        continue;

                    if (p->arriveTime == 0)
                        p->pos = p->arrivePos;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort NextCoord(ushort prev, byte dir)
        {
            switch (dir)
            {
                case UnsafePlayer.DIR_UP:
                    var result = prev + Env.X_CELLS_COUNT;
                    if (result >= Env.CELLS_COUNT)
                        return ushort.MaxValue;
                    return (ushort)result;

                case UnsafePlayer.DIR_LEFT:
                    if (prev % Env.X_CELLS_COUNT == 0)
                        return ushort.MaxValue;
                    return (ushort)(prev - 1);

                case UnsafePlayer.DIR_DOWN:
                    result = prev - Env.X_CELLS_COUNT;
                    if (result < 0)
                        return ushort.MaxValue;
                    return (ushort)result;

                case UnsafePlayer.DIR_RIGHT:
                    if (prev % Env.X_CELLS_COUNT == Env.X_CELLS_COUNT - 1)
                        return ushort.MaxValue;
                    return (ushort)(prev + 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToCoord(V v)
        {
            return (ushort)(v.X + v.Y * Env.X_CELLS_COUNT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V ToV(ushort c)
        {
            return V.Get(c % Env.X_CELLS_COUNT, c / Env.X_CELLS_COUNT);
        }
    }
}