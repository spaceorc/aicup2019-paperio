using System.Runtime.CompilerServices;
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
        public int tickScore;
        public int nitroLeft;
        public int slowLeft;
        public int territory;
        
        public int lineCount;
        public V[] line;

        public FastPlayer(Config config)
        {
            line = new V[config.x_cells_count * config.y_cells_count];
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
            if (lineCount > 0 || state.territory[arrivePos.X, arrivePos.Y] != player)
            {
                line[lineCount++] = arrivePos;
                state.lines[arrivePos.X, arrivePos.Y] = (byte)(state.lines[arrivePos.X, arrivePos.Y] | (1 << player));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RenewArriveTime()
        {
            if (dir == null)
                return;
            
            if (arriveTime == 0)
            {
                arriveTime = shiftTime;
                arrivePos += V.vertAndHoriz[(int)dir];
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