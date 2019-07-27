using System;
using Game.Helpers;
using Game.Types;

namespace BrutalTester
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.ReadLine();

            while (true)
            {
                try
                {
                    if (args[0] == "1")
                    {
                        // WriteScript(6, Direction.Up);
                        // WriteScript(8, Direction.Right);
                        // WriteScript(7, Direction.Down);
                        // WriteScript(3, Direction.Left);
                        // WriteScript(5, Direction.Up);
                        // WriteScript(4, Direction.Left);
                        // WriteScript(4, Direction.Down);
                        // WriteScript(1, Direction.Left);
                        // WriteScript(5, Direction.Up);
                        // WriteScript(15, Direction.Right);
                        // WriteScript(10, Direction.Down);
                        // WriteScript(10, Direction.Left);
                        // WriteScript(4, Direction.Up);
                        // WriteScript(20, Direction.Right, Direction.Down, Direction.Left, Direction.Up);
                        WriteScript(2, Direction.Right, Direction.Down, Direction.Left, Direction.Up);
                        //WriteScript(Direction.Right, Direction.Right, Direction.Down);
                        WriteScript(10, Direction.Right);
                    }

                    if (args[0] == "2")
                    {
                        // WriteScript(7, Direction.Left);
                        // WriteScript(1, Direction.Up);
                        // WriteScript(7, Direction.Right);
                        // WriteScript(16, Direction.Right, Direction.Down, Direction.Left, Direction.Up);
                        // WriteScript(6, Direction.Left);
                        // WriteScript(20, Direction.Down, Direction.Left, Direction.Up, Direction.Right); 
                        
                        WriteScript(6, Direction.Left);
                        WriteScript(1, Direction.Down);
                        WriteScript(7, Direction.Right);
                        WriteScript(100, Direction.Right, Direction.Down, Direction.Left, Direction.Up);
                    }
                    WriteFin();

                }
                catch (Exception e)
                {
                }
            }
        }

        public static void WriteFin()
        {
            WriteScript(100, Direction.Up);
        }

        public static void WriteScript(int repeat, params Direction[] dirs)
        {
            for (int i = 0; i < repeat; i++)
            {
                WriteScript(dirs);
            }
        }

        public static void WriteScript(params Direction[] dirs)
        {
            foreach (var dir in dirs)
            {
                var line = Console.ReadLine();
                if (line == null)
                    Environment.Exit(0);
                if (line.IndexOf("start_game") >= 0)
                    throw new Exception();
                Console.Out.WriteLine(new {command = dir}.ToJson());
            }
        }
    }
}