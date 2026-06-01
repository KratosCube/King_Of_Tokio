using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class VictoryPointsGainedEvent : GameEventBase
{
    public int PlayerId { get; }
    public int Amount { get; }
    public string Reason { get; }

    public VictoryPointsGainedEvent(int playerId, int amount, string reason)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PlayerId = playerId;
        Amount = amount;
        Reason = reason;
    }

    public override string EventName => nameof(VictoryPointsGainedEvent);
}