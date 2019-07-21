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
        
        public static int SPEED = 5;
        public static int WIDTH = 30; // должно делиться на 2
        public static int BONUS_CHANCE = 500; // 1 из BONUS_CHANCE
        public static int BONUSES_MAX_COUNT = 3;
        public static int Y_CELLS_COUNT = 31;
        public static int X_CELLS_COUNT = 31;

        public static int NEUTRAL_TERRITORY_SCORE = 1;
        public static int ENEMY_TERRITORY_SCORE = 5;
        public static int SAW_SCORE = 30;
        public static int LINE_KILL_SCORE = 50;
        public static int SAW_KILL_SCORE = 150;
        
        public static int MAX_EXECUTION_TIME = 120;
        public static int REQUEST_MAX_TIME = 5;
        public static int MAX_TICK_COUNT = 1500;

        public static int WINDOW_WIDTH => WIDTH * X_CELLS_COUNT;
        public static int WINDOW_HEIGHT => WIDTH * Y_CELLS_COUNT;
    }
}