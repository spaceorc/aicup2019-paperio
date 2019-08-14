using System;
using Game.Protocol;

namespace Game.Fast
{
    public class UndoData
    {
        public bool isGameOver;
        
        public int time;
        public int bonusCount;
        public int territoryVersion;
        public bool captured;
        public byte[] territory;
        public UndoPlayerData[] undoPlayerDatas;

        public UndoData(int playerCount)
        {
            undoPlayerDatas = new UndoPlayerData[playerCount];
            for (int i = 0; i < undoPlayerDatas.Length; i++)
                undoPlayerDatas[i] = new UndoPlayerData();
            territory = new byte[Env.CELLS_COUNT];
        }

        public void Before(FastState state)
        {
            captured = false;
            time = state.time;
            bonusCount = state.bonusCount;
            territoryVersion = state.territoryVersion;
            isGameOver = state.isGameOver;
            for (int i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].Before(state, state.players[i]);
        }

        public void NotifyCapture(FastState state)
        {
            if (!captured)
            {
                captured = true;
                Array.Copy(state.territory, territory, territory.Length);
            }
        }

        public void After(FastState state)
        {
            for (int i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].After(state, state.players[i]);
        }

        public void Undo(FastState state)
        {
            state.time = time;
            state.isGameOver = isGameOver;
            for (int i = 0; i < state.players.Length; i++)
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