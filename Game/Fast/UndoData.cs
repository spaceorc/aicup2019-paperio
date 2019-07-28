using System;
using Game.Protocol;

namespace Game.Fast
{
    public class UndoData
    {
        public int time;
        public int bonusCount;
        public int territoryVersion;
        public byte[,] territory;
        public UndoPlayerData[] undoPlayerDatas;

        public UndoData(int playerCount, Config config)
        {
            undoPlayerDatas = new UndoPlayerData[playerCount];
            for (int i = 0; i < undoPlayerDatas.Length; i++)
                undoPlayerDatas[i] = new UndoPlayerData(config);
            territory = new byte[config.x_cells_count, config.y_cells_count];
        }

        public void Before(FastState state)
        {
            time = state.time;
            bonusCount = state.bonusCount;
            territoryVersion = state.territoryVersion;
            for (int i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].Before(state, state.players[i]);
        }

        public void After(FastState state)
        {
            for (int i = 0; i < state.players.Length; i++)
                undoPlayerDatas[i].After(state, state.players[i]);
            if (state.territoryVersion != territoryVersion)
                Array.Copy(state.territory, territory, territory.Length);
        }

        public void Undo(FastState state)
        {
            state.time = time;
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