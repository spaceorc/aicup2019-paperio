using System;
using Game.Fast;
using Game.Helpers;
using Game.Protocol;
using Game.Types;

namespace Game.Strategies
{
    public interface IAi
    {
        RequestOutput GetCommand(FastState state, int player, ITimeManager timeManager, Random random);
    }
}