using System;

namespace Game.Protocol
{
    public static class Env
    {
        static Env()
        {
            foreach (var fieldInfo in typeof(Env).GetFields())
            {
                var value = Environment.GetEnvironmentVariable(fieldInfo.Name);
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var intValue))
                    fieldInfo.SetValue(null, intValue);
            }
        }
        
        public static int MAX_EXECUTION_TIME = 120;
        public static int REQUEST_MAX_TIME = 5;
        public static int MAX_TICK_COUNT = 1500;
    }
}