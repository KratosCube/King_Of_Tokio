using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class EnergyGainedEvent : GameEventBase
{
    public int PlayerId { get; }
    public int Amount { get; }
    public string Reason { get; }

    public EnergyGainedEvent(int playerId, int amount, string reason)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PlayerId = playerId;
        Amount = amount;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public override string EventName => nameof(EnergyGainedEvent);
}