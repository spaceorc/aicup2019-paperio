namespace Game.Helpers
{
    public interface ITimeManager
    {
        bool IsExpired { get; }
        bool BeStupid { get; }
        bool BeSmart { get; }
        bool IsExpiredGlobal { get; }
    }
}