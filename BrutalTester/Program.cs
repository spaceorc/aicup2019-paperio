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
                        // WriteScript(4, Direction.Up);
                        //WriteScript(1, Direction.Left);
                        //WriteScript(1, Direction.Down);
                        // WriteScript(1, Direction.Down);
                        // WriteScript(4, Direction.Left);
                        WriteScript(6, Direction.Up, Direction.Right, Direction.Down, Direction.Left);
                        WriteScript(Direction.Up, Direction.Right, Direction.Down);
                        WriteScript(5, Direction.Right);
                    }

                    if (args[0] == "2")
                    {
                        WriteScript(9, Direction.Down);
                        WriteScript(1, Direction.Right);
                        // WriteScript(1, Direction.Down);
                        // WriteScript(5, Direction.Right);

                        //WriteScript(100, Direction.Up, Direction.Right, Direction.Down, Direction.Left);
                    }

                    if (args[0] == "3")
                    {
                        WriteScript(1, Direction.Up);
                        WriteScript(8, Direction.Left);
                        WriteScript(1, Direction.Down);
                        WriteScript(8, Direction.Right);
                        WriteScript(1, Direction.Up);
                        WriteScript(8, Direction.Left);
                        WriteScript(1, Direction.Down);
                        WriteScript(10, Direction.Left);

                        //WriteScript(100, Direction.Up, Direction.Right, Direction.Down, Direction.Left);
                    }

                    WriteFin();
                }
                catch
                {
                }
            }
        }

        public static void WriteFin()
        {
            WriteScript(100, Direction.Up);
        }

        public static void WriteScript(int repeat, params Direction?[] dirs)
        {
            for (int i = 0; i < repeat; i++)
            {
                WriteScript(dirs);
            }
        }

        public static void WriteScript(params Direction?[] dirs)
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