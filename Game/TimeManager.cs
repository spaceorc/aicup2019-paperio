using System.Diagnostics;
using Game.Protocol;

namespace Game
{
	public class TimeManager
	{
		private readonly Stopwatch stopwatch = new Stopwatch();
		public readonly long totalTime;
		public readonly long totalTicks;
		public long timeElapsed;
		public long ticksPassed;
		public long millisPerTick;
        private bool stupidMode;
        public readonly long maxMillisPerTick;
        public readonly long beStupidMillisPerTick;
        public readonly long beNormalMillisPerTick;
        public readonly long beSmartMillisPerTick;

        public TimeManager()
		{
			totalTime = Env.MAX_EXECUTION_TIME * 1000;
			totalTicks = Env.MAX_TICK_COUNT;
			millisPerTick = totalTime / totalTicks;
            maxMillisPerTick = Env.REQUEST_MAX_TIME * 1000;
            beStupidMillisPerTick = millisPerTick / 2;
            beNormalMillisPerTick = millisPerTick;
            beSmartMillisPerTick = millisPerTick * 2;
        }

		public void TickStarted()
		{
			stopwatch.Restart();
		}

		public void TickFinished()
		{
			stopwatch.Stop();
			timeElapsed += Elapsed;
			ticksPassed++;
			millisPerTick = totalTicks == ticksPassed ? 0 : (totalTime - timeElapsed) / (totalTicks - ticksPassed);
			if (millisPerTick > maxMillisPerTick)
				millisPerTick = maxMillisPerTick;
			else if (!stupidMode && millisPerTick <= beStupidMillisPerTick)
				stupidMode = true;
			else if (stupidMode && millisPerTick >= beNormalMillisPerTick)
				stupidMode = false;
		}

		public bool IsExpired => Elapsed >= millisPerTick;
		public bool BeStupid => stupidMode;
		public bool BeSmart => millisPerTick >= beSmartMillisPerTick;
		public bool IsExpiredGlobal => timeElapsed > totalTime;
		public long Elapsed => stopwatch.ElapsedMilliseconds + 3;

		public override string ToString()
		{
			return $"{nameof(totalTime)}: {totalTime}, {nameof(totalTicks)}: {totalTicks}, {nameof(timeElapsed)}: {timeElapsed}, {nameof(ticksPassed)}: {ticksPassed}, {nameof(millisPerTick)}: {millisPerTick}";
		}
	}
}