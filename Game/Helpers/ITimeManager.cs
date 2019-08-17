using System;

namespace Game.Helpers
{
    public interface ITimeManager
    {
        bool IsExpired { get; }

        ITimeManager GetNested(int millis);
    }
}