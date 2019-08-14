using Game.Protocol;

namespace Game.Sim.Undo
{
    public class UndoPlayerData
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
        private readonly ushort[] line;

        public UndoPlayerData()
        {
            line = new ushort[Env.CELLS_COUNT];
        }

        public void Before(Player player)
        {
            status = player.status;
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
        }

        public void After(Player player)
        {
            if (player.lineCount == 0 && lineCount > 0)
            {
                for (var i = 0; i < lineCount; i++)
                    line[i] = player.line[i];
            }
        }

        public void Undo(State state, Player player, int p)
        {
            if (player.lineCount == 0 && lineCount > 0)
            {
                for (var i = 0; i < lineCount; i++)
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
        }
    }
}