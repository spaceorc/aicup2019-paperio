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

            beStupidMillisPerRequest = totalTime / (totalTicks / config.ticksPerRequest) / 2;
            beNormalMillisPerRequest = totalTime / (totalTicks / config.ticksPerRequest);
            beSmartMillisPerRequest = totalTime / (totalTicks / config.ticksPerRequest) * 2;
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
                requestsLeft += slowTicksLeft / config.ticksPerRequest;
                requestsLeft += (nitroTicksLeft - slowTicksLeft) / config.nitroTicksPerRequest;
                requestsLeft += (ticksLeft - nitroTicksLeft) / config.ticksPerRequest;
            }
            else
            {
                requestsLeft += nitroTicksLeft / config.ticksPerRequest;
                requestsLeft += (slowTicksLeft - nitroTicksLeft) / config.slowTicksPerRequest;
                requestsLeft += (ticksLeft - slowTicksLeft) / config.ticksPerRequest;
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
        public bool BeStupid => stupidMode;
        public bool BeSmart => millisPerRequest >= beSmartMillisPerRequest;
        public bool IsExpiredGlobal => timeElapsed > totalTime;
        public int Elapsed => (int)stopwatch.ElapsedMilliseconds + 3;
        public int ElapsedGlobal => timeElapsed;
    }
}