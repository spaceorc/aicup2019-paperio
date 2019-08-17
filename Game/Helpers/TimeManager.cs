using System;
using System.Diagnostics;
using Game.Protocol;

namespace Game.Helpers
{
    public class TimeManager : ITimeManager
    {
        private readonly Config config;
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly int totalTime;
        private readonly int totalTicks;

        private int timeElapsed;
        private int millisPerRequest;
        private bool stupidMode;
        private readonly int maxMillisPerRequest;
        private readonly int beStupidMillisPerRequest;
        private readonly int beNormalMillisPerRequest;
        private readonly int beSmartMillisPerRequest;

        public TimeManager(Config config)
        {
            this.config = config;
            totalTime = Env.MAX_EXECUTION_TIME * 950;
            maxMillisPerRequest = Env.REQUEST_MAX_TIME * 950;
            totalTicks = Env.MAX_TICK_COUNT;

            beStupidMillisPerRequest = totalTime / (totalTicks / Env.TICKS_PER_REQUEST) / 2;
            beNormalMillisPerRequest = totalTime / (totalTicks / Env.TICKS_PER_REQUEST);
            beSmartMillisPerRequest = totalTime / (totalTicks / Env.TICKS_PER_REQUEST) * 2;
        }

        public void RequestStarted(int tickNum, int nitroTicksLeft, int slowTicksLeft)
        {
            stopwatch.Restart();
            var ticksLeft = totalTicks - tickNum + 1;
            if (nitroTicksLeft > ticksLeft)
                nitroTicksLeft = ticksLeft;
            if (slowTicksLeft > ticksLeft)
                slowTicksLeft = ticksLeft;

            var requestsLeft = 0;
            if (nitroTicksLeft > slowTicksLeft)
            {
                requestsLeft += slowTicksLeft / Env.TICKS_PER_REQUEST;
                requestsLeft += (nitroTicksLeft - slowTicksLeft) / Env.NITRO_TICKS_PER_REQUEST;
                requestsLeft += (ticksLeft - nitroTicksLeft) / Env.TICKS_PER_REQUEST;
            }
            else
            {
                requestsLeft += nitroTicksLeft / Env.TICKS_PER_REQUEST;
                requestsLeft += (slowTicksLeft - nitroTicksLeft) / Env.SLOW_TICKS_PER_REQUEST;
                requestsLeft += (ticksLeft - slowTicksLeft) / Env.TICKS_PER_REQUEST;
            }

            millisPerRequest = requestsLeft == 0 ? 0 : (totalTime - timeElapsed) / requestsLeft;
            if (millisPerRequest > maxMillisPerRequest)
                millisPerRequest = maxMillisPerRequest;
            else if (!stupidMode && millisPerRequest <= beStupidMillisPerRequest)
                stupidMode = true;
            else if (stupidMode && millisPerRequest >= beNormalMillisPerRequest)
                stupidMode = false;
        }

        public void RequestFinished()
        {
            stopwatch.Stop();
            timeElapsed += Elapsed;
        }

        public bool IsExpired => Elapsed >= millisPerRequest;

        public ITimeManager GetNested(int millis) => new SimpleTimeManager(millis);

        public bool BeStupid => stupidMode;
        public bool BeSmart => millisPerRequest >= beSmartMillisPerRequest;
        public bool IsExpiredGlobal => timeElapsed > totalTime;
        public int Elapsed => (int)stopwatch.ElapsedMilliseconds + 3;
        public int ElapsedGlobal => timeElapsed;
    }

    public class SimpleTimeManager : ITimeManager
    {
        private readonly int millis;
        private readonly Stopwatch stopwatch;

        public SimpleTimeManager(int millis)
        {
            this.millis = millis;
            stopwatch = Stopwatch.StartNew();
        }

        public bool IsExpired => Elapsed >= millis;
        public int Elapsed => (int)stopwatch.ElapsedMilliseconds;

        public ITimeManager GetNested(int millis) => new SimpleTimeManager(millis);
    }
}