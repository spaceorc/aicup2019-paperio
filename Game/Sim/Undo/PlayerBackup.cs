using System;
using Game.Protocol;

namespace Game.Sim.Undo
{
    public class PlayerBackup
    {
        private PlayerStatus status;
        private ushort pos;
        private ushort arrivePos;
        private Direction? dir;
        private int arriveTime;
        private int shiftTime;
        private byte killedBy;
        private int score;
        private int nitroLeft;
        private int slowLeft;
        private int territory;
        private int nitrosCollected;
        private int slowsCollected;
        private int opponentTerritoryCaptured;
        private int lineCount;
        private ushort[] line;

        public void Backup(Player player)
        {
            status = player.status;
            if (status == PlayerStatus.Eliminated)
                return;

            pos = player.pos;
            arrivePos = player.arrivePos;
            killedBy = player.killedBy;
            dir = player.dir;
            arriveTime = player.arriveTime;
            shiftTime = player.shiftTime;
            score = player.score;
            nitroLeft = player.nitroLeft;
            slowLeft = player.slowLeft;
            territory = player.territory;
            lineCount = player.lineCount;
            nitrosCollected = player.nitrosCollected;
            slowsCollected = player.slowsCollected;
            opponentTerritoryCaptured = player.opponentTerritoryCaptured;

            if (line == null)
                line = new ushort[player.line.Length];
            
            Array.Copy(player.line, line, lineCount);
        }

        public void Restore(Player player)
        {
            if (status == PlayerStatus.Eliminated)
                return;
            
            player.status = status;
            player.pos = pos;
            player.arrivePos = arrivePos;
            player.killedBy = killedBy;
            player.dir = dir;
            player.arriveTime = arriveTime;
            player.shiftTime = shiftTime;
            player.score = score;
            player.nitroLeft = nitroLeft;
            player.slowLeft = slowLeft;
            player.territory = territory;
            player.lineCount = lineCount;
            player.nitrosCollected = nitrosCollected;
            player.slowsCollected = slowsCollected;
            player.opponentTerritoryCaptured = opponentTerritoryCaptured;
            
            Array.Copy(line, player.line, lineCount);
        }
    }
}