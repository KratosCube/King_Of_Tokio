using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class StatusTokensAddedEvent : GameEventBase
{
    public int SourcePlayerId { get; }
    public int TargetPlayerId { get; }
    public int PoisonTokensAdded { get; }
    public int ShrinkTokensAdded { get; }

    public StatusTokensAddedEvent(
        int sourcePlayerId,
        int targetPlayerId,
        int poisonTokensAdded,
        int shrinkTokensAdded)
    {
        if (poisonTokensAdded < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poisonTokensAdded));
        }

        if (shrinkTokensAdded < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shrinkTokensAdded));
        }

        SourcePlayerId = sourcePlayerId;
        TargetPlayerId = targetPlayerId;
        PoisonTokensAdded = poisonTokensAdded;
        ShrinkTokensAdded = shrinkTokensAdded;
    }

    public override string EventName => nameof(StatusTokensAddedEvent);
}
