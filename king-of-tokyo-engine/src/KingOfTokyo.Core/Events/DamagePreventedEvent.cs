using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Events;

public sealed class DamagePreventedEvent : GameEventBase
{
    public int SourcePlayerId { get; }
    public int TargetPlayerId { get; }
    public int Amount { get; }
    public DamageKind DamageKind { get; }
    public string Reason { get; }

    public DamagePreventedEvent(int sourcePlayerId, int targetPlayerId, int amount, DamageKind damageKind, string reason)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePlayerId);
        ArgumentOutOfRangeException.ThrowIfNegative(targetPlayerId);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        SourcePlayerId = sourcePlayerId;
        TargetPlayerId = targetPlayerId;
        Amount = amount;
        DamageKind = damageKind;
        Reason = reason;
    }

    public override string EventName => nameof(DamagePreventedEvent);
}
