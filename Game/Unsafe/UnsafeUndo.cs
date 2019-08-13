using System.Runtime.CompilerServices;

namespace Game.Unsafe
{
    public unsafe struct UnsafeUndo
    {
        private const int longsToCopy = 31 * 31 / 8;

        public byte mask;
        public ushort time;
        public fixed byte players[6 * UnsafePlayer.Size];
        public fixed byte prevTerritory[9];
        public fixed ushort prevTerritoryPos[9];
        public byte prevTerritoryCount;
        public fixed byte territory[31 * 31];
        public byte territoryChanged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeforeCommands(UnsafeState* state)
        {
            fixed (UnsafeUndo* that = &this)
            {
                that->territoryChanged = 0;
                that->time = state->time;
                that->mask = state->mask;
                that->prevTerritoryCount = 0;
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
        public void AfterCommands(UnsafeState* state)
        {
            fixed (UnsafeUndo* that = &this)
            {
                var p = (UnsafePlayer*)state->players;
                for (int i = 0; i < 6; i++, p++)
                {
                    if (p->status != UnsafePlayer.STATUS_ELIMINATED && p->arrivePos != ushort.MaxValue)
                    {
                        that->prevTerritory[that->prevTerritoryCount] = state->territory[p->arrivePos];
                        that->prevTerritoryPos[that->prevTerritoryCount++] = p->arrivePos;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeforeTerritoryLocalChange(UnsafeState* state, ushort pos)
        {
            fixed (UnsafeUndo* that = &this)
            {
                that->prevTerritory[that->prevTerritoryCount] = state->territory[pos];
                that->prevTerritoryPos[that->prevTerritoryCount++] = pos;
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
                    for (int pt = 0; pt < that->prevTerritoryCount; pt++)
                        state->territory[that->prevTerritoryPos[pt]] = that->prevTerritory[pt];
                }

                state->time = that->time;
                state->mask = that->mask;
            }
        }
    }
}