using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Game.Sim.Undo
{
    public class UndoDataPool
    {
        private readonly int playerCount;
        private readonly Stack<UndoData> stack = new Stack<UndoData>();

        public UndoDataPool(int playerCount)
        {
            this.playerCount = playerCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UndoData Get()
        {
            return stack.Count == 0
                ? new UndoData(playerCount)
                : stack.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(UndoData undo)
        {
            stack.Push(undo);
        }
    }
}