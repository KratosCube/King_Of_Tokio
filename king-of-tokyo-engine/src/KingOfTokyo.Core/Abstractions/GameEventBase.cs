namespace KingOfTokyo.Core.Abstractions;

public abstract class GameEventBase
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
    public abstract string EventName { get; }
}