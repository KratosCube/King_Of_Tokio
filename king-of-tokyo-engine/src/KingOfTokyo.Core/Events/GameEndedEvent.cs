using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class GameEndedEvent : GameEventBase
{
    public int? WinnerPlayerId { get; }
    public bool HasWinner => WinnerPlayerId.HasValue;
    public string Reason { get; }

    public GameEndedEvent(int? winnerPlayerId, string reason)
    {
        WinnerPlayerId = winnerPlayerId;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public override string EventName => nameof(GameEndedEvent);
}