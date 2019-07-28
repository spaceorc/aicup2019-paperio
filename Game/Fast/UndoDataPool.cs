using System.Collections.Generic;
using Game.Protocol;

namespace Game.Fast
{
    public class UndoDataPool
    {
        private readonly int playerCount;
        private readonly Config config;
        private readonly Stack<UndoData> stack = new Stack<UndoData>();

        public UndoDataPool(int playerCount, Config config)
        {
            this.playerCount = playerCount;
            this.config = config;
        }

        public UndoData Get()
        {
            return stack.Count == 0
                ? new UndoData(playerCount, config)
                : stack.Pop();
        }

        public void Return(UndoData undo)
        {
            stack.Push(undo);
        }
    }
}