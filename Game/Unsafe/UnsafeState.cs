using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Protocol;

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

        public const byte MASK_GAME_OVER = 0b10000000;
        public const byte MASK_PLAYERS = 0b00111111;
        public byte mask; // 1

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
        public fixed byte territory[31 * 31]; // 961

        public void NextTurn(UnsafeGame* game, UnsafeCapture* capture)
        {
            fixed (UnsafeState* that = &this)
            {
                var timeDelta = RenewArriveTime();

                that->time = (ushort)(that->time + timeDelta);
                if (that->time > Env.MAX_TICK_COUNT)
                {
                    that->mask |= MASK_GAME_OVER;
                    return;
                }

                Move(timeDelta);

                var tickScores = stackalloc ushort[6];
                for (int i = 0; i < 6; i++)
                    tickScores[i] = 0;

                CheckIntermediateCollisions();

                Capture(capture, tickScores);

                // CheckLoss();
                //
                // CheckIsAte();
            }
        }

        private byte RenewArriveTime()
        {
            fixed (UnsafeState* that = &this)
            {
                var minArriveTime = byte.MaxValue;
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6; i++, p++)
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

        private void CheckIntermediateCollisions()
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6 - 1; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    var pk = p + 1;
                    for (int k = i + 1; k < 6; k++, pk++)
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

        // private void CheckLoss(ushort* tickScores)
        // {
        //     fixed (UnsafeState* that = &this)
        //     {
        //         var p = (UnsafePlayer*)that->players;
        //         for (int i = 0; i < 6; i++, p++)
        //         {
        //             if (p->status == UnsafePlayer.STATUS_ELIMINATED)
        //                 continue;
        //             
        //             if (p->status != UnsafePlayer.STATUS_LOOSER)
        //             {
        //                 if (p->territory == 0)
        //                     p->status = UnsafePlayer.STATUS_LOOSER;
        //                 else if (p->arrivePos == ushort.MaxValue)
        //                     p->status = UnsafePlayer.STATUS_LOOSER;
        //                 else if (p->arriveTime == 0 && that->territory[p->arrivePos] == i << TERRITORY_LINE_SHIFT)
        //                     p->status = UnsafePlayer.STATUS_LOOSER;
        //             }
        //
        //             var pk = p + 1;
        //             for (int k = i + 1; k < 6; k++, pk++)
        //             {
        //                 if (pk->status == UnsafePlayer.STATUS_ELIMINATED)
        //                     continue;
        //
        //                 if (pk->arriveTime == 0 && pk->arrivePos != ushort.MaxValue && that->territory[pk->arrivePos] == i << TERRITORY_LINE_SHIFT)
        //                 {
        //                     p->status = UnsafePlayer.STATUS_LOOSER;
        //                     p->killedBy = (byte)(p->killedBy | (1 << k));
        //                     tickScores[k] = (ushort)(tickScores[k] + Env.LINE_KILL_SCORE);
        //                 }
        //
        //                 if (p->arriveTime == 0 && p->arrivePos != ushort.MaxValue && that->territory[p->arrivePos] == k << TERRITORY_LINE_SHIFT)
        //                 {
        //                     pk->status = UnsafePlayer.STATUS_LOOSER;
        //                     pk->killedBy = (byte)(pk->killedBy | (1 << i));
        //                     tickScores[i] = (ushort)(tickScores[i] + Env.LINE_KILL_SCORE);
        //                 }
        //             }
        //         }
        //     }
        //
        //
        //     for (int i = 0; i < players.Length; i++)
        //     {
        //         if (players[i].status == PlayerStatus.Eliminated)
        //             continue;
        //
        //         if (players[i].arriveTime == 0 && players[i].arrivePos != ushort.MaxValue && capture.territoryCaptureCount[i] == 0)
        //             players[i].UpdateLines(i, this);
        //     }
        //
        //     for (int i = 0; i < players.Length - 1; i++)
        //     {
        //         if (players[i].status == PlayerStatus.Eliminated)
        //             continue;
        //
        //         for (int k = i + 1; k < players.Length; k++)
        //         {
        //             if (players[k].status == PlayerStatus.Eliminated)
        //                 continue;
        //
        //             if ((players[i].status == PlayerStatus.Loser || players[i].lineCount < players[k].lineCount)
        //                 && (players[k].status == PlayerStatus.Loser || players[k].lineCount < players[i].lineCount))
        //                 continue;
        //
        //             var collides = false;
        //             if (players[i].arrivePos == players[k].arrivePos)
        //                 collides = true;
        //             else
        //             {
        //                 if (players[i].arrivePos == players[k].pos && players[k].arriveTime > 0)
        //                 {
        //                     if (players[i].dir != players[k].dir)
        //                         collides = true;
        //                     else
        //                     {
        //                         var distArrive1 = players[i].arriveTime * players[k].shiftTime;
        //                         var distArrive2 = players[k].arriveTime * players[i].shiftTime;
        //                         collides = distArrive1 < distArrive2;
        //                     }
        //                 }
        //                 else if (players[k].arrivePos == players[i].pos && players[i].arriveTime > 0)
        //                 {
        //                     if (players[k].dir != players[i].dir)
        //                         collides = true;
        //                     else
        //                     {
        //                         var distArrive1 = players[i].arriveTime * players[k].shiftTime;
        //                         var distArrive2 = players[k].arriveTime * players[i].shiftTime;
        //                         collides = distArrive2 < distArrive1;
        //                     }
        //                 }
        //             }
        //
        //             if (collides)
        //             {
        //                 if (players[i].status != PlayerStatus.Loser && players[i].lineCount >= players[k].lineCount)
        //                 {
        //                     players[i].status = PlayerStatus.Loser;
        //                     players[i].killedBy = (byte)(players[i].killedBy | (1 << k));
        //                 }
        //
        //                 if (players[k].status != PlayerStatus.Loser && players[k].lineCount >= players[i].lineCount)
        //                 {
        //                     players[k].status = PlayerStatus.Loser;
        //                     players[k].killedBy = (byte)(players[k].killedBy | (1 << i));
        //                 }
        //             }
        //         }
        //     }
        // }

        private void Capture(UnsafeCapture* capture, ushort* tickScores)
        {
            fixed (UnsafeState* that = &this)
            {
                capture->Clear();
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6; i++, p++)
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
                for (int i = 0; i < 6; i++, p++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (p->dir == UnsafePlayer.DIR_NULL)
                        continue;

                    p->arriveTime = (byte)(p->arriveTime - timeDelta);
                }
            }
        }

        private void MoveDone(byte timeDelta)
        {
            fixed (UnsafeState* that = &this)
            {
                var p = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6; i++, p++)
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
                    var result = prev + 31;
                    if (result >= 31 * 31)
                        return ushort.MaxValue;
                    return (ushort)result;

                case UnsafePlayer.DIR_LEFT:
                    if (prev % 31 == 0)
                        return ushort.MaxValue;
                    return (ushort)(prev - 1);

                case UnsafePlayer.DIR_DOWN:
                    result = prev - 31;
                    if (result < 0)
                        return ushort.MaxValue;
                    return (ushort)result;

                case UnsafePlayer.DIR_RIGHT:
                    if (prev % 31 == 31 - 1)
                        return ushort.MaxValue;
                    return (ushort)(prev + 1);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }
    }
}