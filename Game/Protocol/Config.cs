namespace Game.Protocol
{
    public class Config
    {
        public int x_cells_count;
        public int y_cells_count;
        public int speed;
        public int width;
        
        public int ticksPerRequest;
        public int nitroTicksPerRequest;
        public int slowTicksPerRequest;
        public int nitroSpeed;
        public int slowSpeed;

        public void Prepare()
        {
            nitroSpeed = speed;
            while (nitroSpeed < width)
            {
                nitroSpeed++;
                if (nitroSpeed % width == 0)
                    break;
            }
            slowSpeed = speed;
            while (slowSpeed > 1)
            {
                slowSpeed--;
                if (slowSpeed % width == 0)
                    break;
            }

            ticksPerRequest = width / speed;
            nitroTicksPerRequest = width / nitroSpeed;
            slowTicksPerRequest = width / slowSpeed;
        }
    }
}