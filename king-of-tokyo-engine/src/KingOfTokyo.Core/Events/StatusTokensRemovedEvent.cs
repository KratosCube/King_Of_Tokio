using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class StatusTokensRemovedEvent : GameEventBase
{
    public int PlayerId { get; }
    public int PoisonTokensRemoved { get; }
    public int ShrinkTokensRemoved { get; }
    public int HeartsSpent { get; }

    public StatusTokensRemovedEvent(
        int playerId,
        int poisonTokensRemoved,
        int shrinkTokensRemoved,
        int heartsSpent)
    {
        if (poisonTokensRemoved < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poisonTokensRemoved));
        }

        if (shrinkTokensRemoved < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shrinkTokensRemoved));
        }

        if (heartsSpent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heartsSpent));
        }

        PlayerId = playerId;
        PoisonTokensRemoved = poisonTokensRemoved;
        ShrinkTokensRemoved = shrinkTokensRemoved;
        HeartsSpent = heartsSpent;
    }

    public override string EventName => nameof(StatusTokensRemovedEvent);
}
