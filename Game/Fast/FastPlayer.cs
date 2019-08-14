using System.Runtime.CompilerServices;
using Game.Protocol;

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
        public byte killedBy;

        public int score;
        public int tickScore;
        public int nitroLeft;
        public int slowLeft;
        public int territory;
        public int nitrosCollected;
        public int slowsCollected;
        public int opponentTerritoryCaptured;
        
        public int lineCount;
        public ushort[] line;

        public FastPlayer()
        {
            line = new ushort[Env.CELLS_COUNT];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TickAction()
        {
            if (slowLeft > 0)
                slowLeft--;
            if (nitroLeft > 0)
                nitroLeft--;
            UpdateShiftTime();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateShiftTime()
        {
            shiftTime = GetShiftTime(nitroLeft, slowLeft);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetShiftTime(int nitroLeft, int slowLeft)
        {
            if (slowLeft > 0 && nitroLeft > 0)
                return Env.TICKS_PER_REQUEST;
            if (slowLeft > 0)
                return Env.SLOW_TICKS_PER_REQUEST;
            if (nitroLeft > 0)
                return Env.NITRO_TICKS_PER_REQUEST;
            return Env.TICKS_PER_REQUEST;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateLines(int player, FastState state)
        {
            if (lineCount > 0 || state.territory[arrivePos] != player)
            {
                if ((state.lines[arrivePos] & (1 << player)) == 0)
                {
                    line[lineCount++] = arrivePos;
                    state.lines[arrivePos] = (byte)(state.lines[arrivePos] | (1 << player));
                }
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