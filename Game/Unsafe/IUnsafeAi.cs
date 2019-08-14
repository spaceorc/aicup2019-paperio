using System;
using Game.Helpers;
using Game.Protocol;

namespace Game.Unsafe
{
    public unsafe interface IUnsafeAi
    {
        RequestOutput GetCommand(UnsafeState* state, int player, ITimeManager timeManager, Random random);
    }
}