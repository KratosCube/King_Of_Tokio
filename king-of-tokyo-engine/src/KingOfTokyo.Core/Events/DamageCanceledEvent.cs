using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class DamageCanceledEvent : GameEventBase
{
    public int PlayerId { get; }
    public int Amount { get; }
    public string Reason { get; }

    public DamageCanceledEvent(int playerId, int amount, string reason)
    {
        if (playerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId));
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PlayerId = playerId;
        Amount = amount;
        Reason = reason;
    }

    public override string EventName => nameof(DamageCanceledEvent);
}
