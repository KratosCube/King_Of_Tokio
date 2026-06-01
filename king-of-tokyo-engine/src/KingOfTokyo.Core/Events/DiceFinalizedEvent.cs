using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Events;

public sealed class DiceFinalizedEvent : GameEventBase
{
    public int PlayerId { get; }
    public DiceResolutionSummary Summary { get; }

    public DiceFinalizedEvent(int playerId, DiceResolutionSummary summary)
    {
        PlayerId = playerId;
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    public override string EventName => nameof(DiceFinalizedEvent);
}