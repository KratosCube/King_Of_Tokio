using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class KeepCardDiscardedEvent : GameEventBase
{
    public int PlayerId { get; }
    public string CardId { get; }
    public string CardName { get; }
    public string Reason { get; }

    public KeepCardDiscardedEvent(int playerId, string cardId, string cardName, string reason)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        if (string.IsNullOrWhiteSpace(cardName))
        {
            throw new ArgumentException("Card name must not be empty.", nameof(cardName));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason must not be empty.", nameof(reason));
        }

        PlayerId = playerId;
        CardId = cardId;
        CardName = cardName;
        Reason = reason;
    }

    public override string EventName => nameof(KeepCardDiscardedEvent);
}