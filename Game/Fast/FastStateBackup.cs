using System;

namespace Game.Fast
{
    public class FastStateBackup
    {
        public bool isGameOver;

        public int time;

        public int territoryVersion;
        public byte[] territory;
        public byte[] lines;

        public int bonusCount;
        public FastBonusBackup[] bonuses;

        public FastPlayerBackup[] players;

        public void Backup(FastState state)
        {
            if (territory == null)
            {
                territory = new byte[state.territory.Length];
                lines = new byte[state.lines.Length];
                players = new FastPlayerBackup[state.players.Length];
                for (int i = 0; i < players.Length; i++)
                    players[i] = new FastPlayerBackup();
            }

            if (bonuses == null || bonuses.Length < state.bonusCount)
            {
                bonuses = new FastBonusBackup[state.bonusCount];
                for (int i = 0; i < bonuses.Length; i++)
                    bonuses[i] = new FastBonusBackup();
            }

            isGameOver = state.isGameOver;
            time = state.time;
            territoryVersion = state.territoryVersion;
            bonusCount = state.bonusCount;
            Array.Copy(state.territory, territory, state.territory.Length);
            Array.Copy(state.lines, lines, state.territory.Length);
            for (int i = 0; i < bonusCount; i++)
                bonuses[i].Backup(state.bonuses[i]);
            for (int i = 0; i < state.players.Length; i++)
                players[i].Backup(state.players[i]);
        }

        public void Restore(FastState state)
        {
            state.isGameOver = isGameOver;
            state.time = time;
            state.territoryVersion = territoryVersion;
            state.bonusCount = bonusCount;
            Array.Copy(territory, state.territory, state.territory.Length);
            Array.Copy(lines, state.lines, state.territory.Length);
            for (int i = 0; i < bonusCount; i++)
                bonuses[i].Restore(state.bonuses[i]);
            for (int i = 0; i < state.players.Length; i++)
                players[i].Restore(state.players[i]);
        }
    }
}