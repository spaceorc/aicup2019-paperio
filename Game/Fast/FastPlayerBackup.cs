using System;
using Game.Types;

namespace Game.Fast
{
    public class FastPlayerBackup
    {
        public PlayerStatus status;
        public ushort pos;
        public ushort arrivePos;
        public Direction? dir;
        public int arriveTime;
        public int shiftTime;
        public byte killedBy;

        public int score;
        public int tickScore;
        public int nitroLeft;
        public int slowLeft;
        public int territory;

        public int lineCount;
        public ushort[] line;

        public void Backup(FastPlayer player)
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
            tickScore = player.tickScore;
            nitroLeft = player.nitroLeft;
            slowLeft = player.slowLeft;
            territory = player.territory;
            lineCount = player.lineCount;

            if (line == null)
                line = new ushort[player.line.Length];
            
            Array.Copy(player.line, line, lineCount);
        }

        public void Restore(FastPlayer player)
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
            player.tickScore = tickScore;
            player.nitroLeft = nitroLeft;
            player.slowLeft = slowLeft;
            player.territory = territory;
            player.lineCount = lineCount;
            
            Array.Copy(line, player.line, lineCount);
        }
    }
}