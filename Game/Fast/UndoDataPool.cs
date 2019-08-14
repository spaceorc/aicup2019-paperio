using System.Collections.Generic;

namespace Game.Fast
{
    public class UndoDataPool
    {
        private readonly int playerCount;
        private readonly Stack<UndoData> stack = new Stack<UndoData>();

        public UndoDataPool(int playerCount)
        {
            this.playerCount = playerCount;
        }

        public UndoData Get()
        {
            return stack.Count == 0
                ? new UndoData(playerCount)
                : stack.Pop();
        }

        public void Return(UndoData undo)
        {
            stack.Push(undo);
        }
    }
}