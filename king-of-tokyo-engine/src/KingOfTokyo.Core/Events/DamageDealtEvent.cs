using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Events;

public sealed class DamageDealtEvent : GameEventBase
{
    public int SourcePlayerId { get; }
    public int TargetPlayerId { get; }
    public int Amount { get; }
    public DamageKind DamageKind { get; }

    public DamageDealtEvent(int sourcePlayerId, int targetPlayerId, int amount, DamageKind damageKind)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        SourcePlayerId = sourcePlayerId;
        TargetPlayerId = targetPlayerId;
        Amount = amount;
        DamageKind = damageKind;
    }

    public override string EventName => nameof(DamageDealtEvent);
}