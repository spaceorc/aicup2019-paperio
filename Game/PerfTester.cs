using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Protocol;
using Game.Sim;

namespace Game
{
    public class PerfTester
    {
        public int Run()
        {
            var state = new State();
            state.SetInput(
                new RequestInput
                {
                    tick_num = 1,
                    players = new Dictionary<string, RequestInput.PlayerData>
                    {
                        {
                            "i", new RequestInput.PlayerData
                            {
                                territory = new[] {V.Get(0, 0).ToRealCoords()},
                                lines = Enumerable.Range(1, 30)
                                    .Select(x => V.Get(x, 0).ToRealCoords())
                                    .Concat(Enumerable.Range(1, 30).Select(y => V.Get(30, y).ToRealCoords()))
                                    .Concat(Enumerable.Range(0, 30).Select(x => V.Get(29 - x, 30).ToRealCoords()))
                                    .Concat(Enumerable.Range(0, 29).Select(y => V.Get(0, 29 - y).ToRealCoords()))
                                    .ToArray(),
                                position = V.Get(0, 1).ToRealCoords(),
                                direction = Direction.Down,
                                bonuses = new RequestInput.BonusState[0]
                            }
                        }
                    },
                    bonuses = new RequestInput.BonusData[0]
                });

            var commands = new[] {Direction.Down};
            var undo = state.NextTurn(commands, true);

            state.Undo(undo);

            var sw = Stopwatch.StartNew();

            var counter = 0;
            while (sw.ElapsedMilliseconds < 1000)
            {
                undo = state.NextTurn(commands, true);
                state.Undo(undo);
                counter++;
            }

            return counter;
        }
    }
}