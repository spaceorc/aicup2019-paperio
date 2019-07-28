using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class UndoPlayerData
    {
        public PlayerStatus status;
        public ushort pos;
        public ushort arrivePos;
        public Direction? dir;
        public int arriveTime;
        public int shiftTime;

        public int score;
        public int nitroLeft;
        public int slowLeft;
        public int territory;

        public int lineCount;
        public ushort[] line;

        public UndoPlayerData(Config config)
        {
            line = new ushort[config.x_cells_count * config.y_cells_count];
        }

        public void Before(FastState state, FastPlayer player)
        {
            status = player.status;
            pos = player.pos;
            arrivePos = player.arrivePos;
            dir = player.dir;
            arriveTime = player.arriveTime;
            shiftTime = player.shiftTime;
            score = player.score;
            nitroLeft = player.nitroLeft;
            slowLeft = player.slowLeft;
            territory = player.territory;
            lineCount = player.lineCount;
        }

        public void After(FastState state, FastPlayer player)
        {
            if (player.lineCount == 0 && lineCount > 0)
            {
                for (int i = 0; i < lineCount; i++)
                    line[i] = player.line[i];
            }
        }

        public void Undo(FastState state, FastPlayer player, int p)
        {
            player.status = status;
            player.pos = pos;
            player.arrivePos = arrivePos;
            player.dir = dir;
            player.arriveTime = arriveTime;
            player.shiftTime = shiftTime;
            player.score = score;
            player.nitroLeft = nitroLeft;
            player.slowLeft = slowLeft;
            player.territory = territory;
            if (player.lineCount == 0 && lineCount > 0)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    player.line[i] = line[i];
                    var lv = line[i];
                    state.lines[lv] = (byte)(state.lines[lv] | (1 << p));
                }
            }
            else if (lineCount == player.lineCount - 1)
            {
                state.lines[player.arrivePos] = (byte)(state.lines[player.arrivePos] & ~(1 << p));
            }

            player.lineCount = lineCount;
        }
    }
}