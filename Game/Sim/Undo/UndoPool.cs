using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Game.Sim.Undo
{
    public class UndoPool
    {
        private readonly int playerCount;
        private readonly Stack<StateUndo> stack = new Stack<StateUndo>();

        public UndoPool(int playerCount)
        {
            this.playerCount = playerCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateUndo Get()
        {
            return stack.Count == 0
                ? new StateUndo(playerCount)
                : stack.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(StateUndo undo)
        {
            undo.prevUndo = null;
            stack.Push(undo);
        }
    }
}