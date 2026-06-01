using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Events;

public sealed class CardBoughtEvent : GameEventBase
{
    public int PlayerId { get; }
    public string CardId { get; }
    public string CardName { get; }
    public int Cost { get; }
    public MarketCardType CardType { get; }

    public CardBoughtEvent(int playerId, string cardId, string cardName, int cost, MarketCardType cardType)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        if (string.IsNullOrWhiteSpace(cardName))
        {
            throw new ArgumentException("Card name must not be empty.", nameof(cardName));
        }

        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost));
        }

        PlayerId = playerId;
        CardId = cardId;
        CardName = cardName;
        Cost = cost;
        CardType = cardType;
    }

    public override string EventName => nameof(CardBoughtEvent);
}