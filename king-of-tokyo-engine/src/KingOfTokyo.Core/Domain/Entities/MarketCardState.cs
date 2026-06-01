using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Domain.Entities;

public sealed class MarketCardState
{
    public string CardId { get; }
    public string Name { get; }
    public string Description { get; }
    public int Cost { get; }
    public MarketCardType CardType { get; }
    public CardPurchaseEffect PurchaseEffect { get; }

    public MarketCardState(
        string cardId,
        string name,
        string description,
        int cost,
        MarketCardType cardType,
        CardPurchaseEffect? purchaseEffect = null)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Card name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Card description must not be empty.", nameof(description));
        }

        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Card cost must not be negative.");
        }

        CardId = cardId;
        Name = name;
        Description = description;
        Cost = cost;
        CardType = cardType;
        PurchaseEffect = purchaseEffect ?? CardPurchaseEffect.None;
    }
}