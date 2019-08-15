namespace Game.Protocol
{
    public class Config
    {
        public int x_cells_count;
        public int y_cells_count;
        public int speed;
        public int width;
        public int? max_execution_time;
        public int? request_max_time;
        public int? max_tick_count;

        public void ApplyToEnv()
        {
            if (max_execution_time != null)
                Env.MAX_EXECUTION_TIME = max_execution_time.Value;
            if (request_max_time != null)
                Env.REQUEST_MAX_TIME = request_max_time.Value;
            if (max_tick_count != null)
                Env.MAX_TICK_COUNT = max_tick_count.Value;
        }
    }
}