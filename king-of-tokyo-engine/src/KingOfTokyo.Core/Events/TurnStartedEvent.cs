using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class TurnStartedEvent : GameEventBase
{
    public int PlayerId { get; }

    public TurnStartedEvent(int playerId)
    {
        PlayerId = playerId;
    }

    public override string EventName => nameof(TurnStartedEvent);
}