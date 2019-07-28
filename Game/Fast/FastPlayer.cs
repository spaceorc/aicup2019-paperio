using System.Runtime.CompilerServices;
using Game.Protocol;
using Game.Types;

namespace Game.Fast
{
    public class FastPlayer
    {
        public PlayerStatus status;
        public ushort pos;
        public ushort arrivePos;
        public Direction? dir;
        public int arriveTime;
        public int shiftTime;

        public int score;
        public int tickScore;
        public int nitroLeft;
        public int slowLeft;
        public int territory;
        
        public int lineCount;
        public ushort[] line;

        public FastPlayer(Config config)
        {
            line = new ushort[config.x_cells_count * config.y_cells_count];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TickAction(Config config)
        {
            if (slowLeft > 0)
                slowLeft--;
            if (nitroLeft > 0)
                nitroLeft--;
            UpdateBonusEffect(config);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLines(int player, FastState state)
        {
            if (lineCount > 0 || state.territory[arrivePos] != player)
            {
                line[lineCount++] = arrivePos;
                state.lines[arrivePos] = (byte)(state.lines[arrivePos] | (1 << player));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Move(int time)
        {
            if (dir == null)
                return;
            
            arriveTime -= time;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveDone()
        {
            if (dir == null)
                return;
            
            if (arriveTime == 0)
                pos = arrivePos;
        }
    }
}