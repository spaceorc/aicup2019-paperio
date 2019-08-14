using System;
using Game.Helpers;
using Game.Protocol;
using Game.Sim;

namespace Game.Strategies
{
    public interface IAi
    {
        RequestOutput GetCommand(State state, int player, ITimeManager timeManager, Random random);
    }
}