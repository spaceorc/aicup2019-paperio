using System;

namespace Game.Sim.Undo
{
    public class StateBackup
    {
        private bool isGameOver;
        private int time;
        private int territoryVersion;
        private byte[] territory;
        private byte[] lines;
        private int bonusCount;
        private BonusBackup[] bonuses;
        private PlayerBackup[] players;

        public void Backup(State state)
        {
            if (territory == null)
            {
                territory = new byte[state.territory.Length];
                lines = new byte[state.lines.Length];
                players = new PlayerBackup[state.players.Length];
                for (var i = 0; i < players.Length; i++)
                    players[i] = new PlayerBackup();
            }

            if (bonuses == null || bonuses.Length < state.bonusCount)
            {
                bonuses = new BonusBackup[state.bonusCount];
                for (var i = 0; i < bonuses.Length; i++)
                    bonuses[i] = new BonusBackup();
            }

            isGameOver = state.isGameOver;
            time = state.time;
            territoryVersion = state.territoryVersion;
            bonusCount = state.bonusCount;
            Array.Copy(state.territory, territory, state.territory.Length);
            Array.Copy(state.lines, lines, state.territory.Length);
            for (var i = 0; i < bonusCount; i++)
                bonuses[i].Backup(state.bonuses[i]);
            for (var i = 0; i < state.players.Length; i++)
                players[i].Backup(state.players[i]);
        }

        public void Restore(State state)
        {
            state.isGameOver = isGameOver;
            state.time = time;
            state.territoryVersion = territoryVersion;
            state.bonusCount = bonusCount;
            Array.Copy(territory, state.territory, state.territory.Length);
            Array.Copy(lines, state.lines, state.territory.Length);
            for (var i = 0; i < bonusCount; i++)
                bonuses[i].Restore(state.bonuses[i]);
            for (var i = 0; i < state.players.Length; i++)
                players[i].Restore(state.players[i]);
        }
    }
}