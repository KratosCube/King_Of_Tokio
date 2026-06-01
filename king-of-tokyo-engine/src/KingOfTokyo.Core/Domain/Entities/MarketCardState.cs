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
    public int Counters { get; private set; }
    public int StoredEnergy { get; private set; }

    public MarketCardState(
        string cardId,
        string name,
        string description,
        int cost,
        MarketCardType cardType,
        CardPurchaseEffect? purchaseEffect = null,
        int counters = 0,
        int storedEnergy = 0)
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

        if (counters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(counters), "Card counters must not be negative.");
        }

        if (storedEnergy < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storedEnergy), "Stored energy must not be negative.");
        }

        CardId = cardId;
        Name = name;
        Description = description;
        Cost = cost;
        CardType = cardType;
        PurchaseEffect = purchaseEffect ?? CardPurchaseEffect.None;
        Counters = counters;
        StoredEnergy = storedEnergy;
    }

    public void AddCounters(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Counters += amount;
    }

    public void SpendCounters(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (amount > Counters)
        {
            throw new InvalidOperationException("Cannot spend more counters than the card has.");
        }

        Counters -= amount;
    }

    public void AddStoredEnergy(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        StoredEnergy += amount;
    }

    public void SpendStoredEnergy(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (amount > StoredEnergy)
        {
            throw new InvalidOperationException("Cannot spend more stored energy than the card has.");
        }

        StoredEnergy -= amount;
    }
}
