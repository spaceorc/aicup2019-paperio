using System;
using Game.Fast;
using Game.Helpers;
using Game.Types;

namespace Game.Strategies
{
    public interface IAi
    {
        Direction? GetCommand(FastState state, int player, ITimeManager timeManager, Random random);
    }
}