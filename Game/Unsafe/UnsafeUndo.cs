using System.Runtime.CompilerServices;

namespace Game.Unsafe
{
    public unsafe struct UnsafeUndo
    {
        private const int longsToCopy = 31 * 31 / 8;

        public byte mask;
        public ushort time;
        public fixed byte players[6 * UnsafePlayer.Size];
        public fixed byte territory[31 * 31];
        public byte bonusUndoCount;
        public fixed byte bonusTypes[3];
        public fixed ushort bonusPositions[3];
        public byte territoryChanged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Before(UnsafeState* state)
        {
            fixed (UnsafeUndo* that = &this)
            {
                that->territoryChanged = 0;
                that->time = state->time;
                that->mask = state->mask;
                that->bonusUndoCount = 0;
                var p = (UnsafePlayer*)state->players;
                var pu = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6; i++, p++, pu++)
                {
                    if (p->status == UnsafePlayer.STATUS_ELIMINATED)
                        pu->status = UnsafePlayer.STATUS_ELIMINATED;
                    else
                        *pu = *p;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BonusCaptured(ushort pos, byte bonus)
        {
            fixed (UnsafeUndo* that = &this)
            {
                that->bonusTypes[that->bonusUndoCount] = bonus;
                that->bonusPositions[that->bonusUndoCount++] = pos;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeforeTerritoryChange(UnsafeState* state)
        {
            if (territoryChanged != 0)
                return;
            territoryChanged = 1;
            fixed (UnsafeUndo* that = &this)
            {
                var pl = (long*)state->territory;
                var plu = (long*)that->territory;
                for (int i = 0; i < longsToCopy; i++, pl++, plu++)
                    *plu = *pl;
                *(byte*)plu = *(byte*)pl;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Undo(UnsafeState* state)
        {
            fixed (UnsafeUndo* that = &this)
            {
                var p = (UnsafePlayer*)state->players;
                var pu = (UnsafePlayer*)that->players;
                for (int i = 0; i < 6; i++, p++, pu++)
                {
                    if (pu->status == UnsafePlayer.STATUS_ELIMINATED)
                        continue;

                    if (territoryChanged == 0 && pu->lineCount == p->lineCount - 1)
                        state->territory[p->arrivePos] = (byte)(state->territory[p->arrivePos] | UnsafeState.TERRITORY_LINE_NO);

                    *p = *pu;
                }

                if (territoryChanged == 0)
                {
                    var pl = (long*)state->territory;
                    var plu = (long*)that->territory;
                    for (int i = 0; i < longsToCopy; i++, plu++, pl++)
                        *pl = *plu;
                    *(byte*)pl = *(byte*)plu;
                }
                else
                {
                    for (int b = 0; b < that->bonusUndoCount; b++)
                        state->territory[that->bonusPositions[b]] = (byte)(state->territory[that->bonusPositions[b]] | that->bonusTypes[b]);
                }

                state->time = that->time;
                state->mask = that->mask;
            }
        }
    }
}