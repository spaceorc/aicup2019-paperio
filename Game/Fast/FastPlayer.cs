using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastPlayer
    {
        public PlayerStatus status;
        public V pos;
        public V arrivePos;
        public Direction? dir;
        public int arriveTime;
        public int shiftTime;

        public int score;
        public int nitroLeft;
        public int slowLeft;
        public int territory;

        public int lineCount;
        public V[] line;

        public void TickAction(Config config)
        {
            if (slowLeft > 0)
                slowLeft--;
            if (nitroLeft > 0)
                nitroLeft--;
            UpdateBonusEffect(config);
        }

        public void UpdateBonusEffect(Config config)
        {
            if (slowLeft > 0 && nitroLeft > 0)
                shiftTime = config.ticksPerRequest;
            else if (slowLeft > 0)
                shiftTime = config.slowTicksPerRequest;
            else if (nitroLeft > 0)
                shiftTime = config.nitroTicksPerRequest;
            else
                shiftTime = config.ticksPerRequest;
        }

        public void UpdateLines(int player, FastState state)
        {
            if (lineCount > 0 || state.territory[pos.X, pos.Y] != player)
            {
                line[lineCount++] = pos;
                state.lines[pos.X, pos.Y] = (byte)(state.lines[pos.X, pos.Y] | (1 << player));
            }
        }
    }
}