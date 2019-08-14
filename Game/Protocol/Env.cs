namespace Game.Protocol
{
    public static class Env
    {
        public const int SPEED = 5;
        public const int WIDTH = 30; // должно делиться на 2
        public const int BONUS_CHANCE = 500; // 1 из BONUS_CHANCE
        public const int BONUSES_MAX_COUNT = 3;
        public const int Y_CELLS_COUNT = 31;
        public const int X_CELLS_COUNT = 31;

        public const int NEUTRAL_TERRITORY_SCORE = 1;
        public const int ENEMY_TERRITORY_SCORE = 5;
        public const int SAW_SCORE = 30;
        public const int LINE_KILL_SCORE = 50;
        public const int SAW_KILL_SCORE = 150;
        
        public const int MAX_EXECUTION_TIME = 200;
        public const int REQUEST_MAX_TIME = 9;
        public const int MAX_TICK_COUNT = 2500;

        public const int WINDOW_WIDTH = WIDTH * X_CELLS_COUNT;
        public const int WINDOW_HEIGHT = WIDTH * Y_CELLS_COUNT;

        public const int TICKS_PER_REQUEST = 6;
        public const int NITRO_TICKS_PER_REQUEST = 5;
        public const int SLOW_TICKS_PER_REQUEST = 10;
        
        public const int NITRO_SPEED = 6;
        public const int SLOW_SPEED = 3;
        
        public const int CELLS_COUNT = Y_CELLS_COUNT * X_CELLS_COUNT;
    }
}