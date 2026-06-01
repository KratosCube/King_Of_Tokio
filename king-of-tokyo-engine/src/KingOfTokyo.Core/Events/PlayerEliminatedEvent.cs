using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class PlayerEliminatedEvent : GameEventBase
{
    public int EliminatedPlayerId { get; }
    public int? EliminatedByPlayerId { get; }
    public string Reason { get; }

    public PlayerEliminatedEvent(int eliminatedPlayerId, int? eliminatedByPlayerId, string reason)
    {
        EliminatedPlayerId = eliminatedPlayerId;
        EliminatedByPlayerId = eliminatedByPlayerId;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public override string EventName => nameof(PlayerEliminatedEvent);
}