using System;
using Game.Protocol;

namespace Game.Sim.Undo
{
    public class StateUndo
    {
        private bool isGameOver;        
        private int time;
        private int bonusCount;
        private int territoryVersion;
        private bool captured;
        private readonly byte[] territory;
        private readonly PlayerUndo[] undoPlayerDatas;

        public StateUndo(int playerCount)
        {
            undoPlayerDatas = new PlayerUndo[playerCount];
            for (var i = 0; i < undoPlayerDatas.Length; i++)
                undoPlayerDatas[i] = new PlayerUndo();
            territory = new byte[Env.CELLS_COUNT];
        }

        public void Before(State state)
        {
            captured = false;
            time = state.time;
            bonusCount = state.bonusCount;
            territoryVersion = state.territoryVersion;
            isGameOver = state.isGameOver;
            for (var i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].Before(state.players[i]);
        }

        public void NotifyCapture(State state)
        {
            if (!captured)
            {
                captured = true;
                Array.Copy(state.territory, territory, territory.Length);
            }
        }

        public void After(State state)
        {
            for (var i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].After(state.players[i]);
        }

        public void Undo(State state)
        {
            state.time = time;
            state.isGameOver = isGameOver;
            for (var i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].Undo(state, state.players[i], i);
            state.bonusCount = bonusCount;
            if (state.territoryVersion != territoryVersion)
            {
                state.territoryVersion = territoryVersion;
                Array.Copy(territory, state.territory, territory.Length);
            }
        }
    }
}