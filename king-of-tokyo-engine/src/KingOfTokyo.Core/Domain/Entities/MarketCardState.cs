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
    public MimicTargetState? MimicTarget { get; private set; }

    public MarketCardState(
        string cardId,
        string name,
        string description,
        int cost,
        MarketCardType cardType,
        CardPurchaseEffect? purchaseEffect = null,
        int counters = 0,
        int storedEnergy = 0,
        MimicTargetState? mimicTarget = null)
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
        MimicTarget = mimicTarget;
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

    public void ResetCounters()
    {
        Counters = 0;
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

    public void SetMimicTarget(MimicTargetState target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (CardId != KnownCardIds.Mimic)
        {
            throw new InvalidOperationException("Only Mimic cards can copy another card.");
        }

        MimicTarget = target;
    }

    public void ClearMimicTarget()
    {
        if (CardId != KnownCardIds.Mimic)
        {
            throw new InvalidOperationException("Only Mimic cards can clear a copied card target.");
        }

        MimicTarget = null;
    }
}

public sealed record MimicTargetState
{
    public int OwnerPlayerId { get; }
    public string CardId { get; }
    public string CardName { get; }

    public MimicTargetState(int ownerPlayerId, string cardId, string cardName)
    {
        if (ownerPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerPlayerId));
        }

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Copied card id must not be empty.", nameof(cardId));
        }

        if (string.IsNullOrWhiteSpace(cardName))
        {
            throw new ArgumentException("Copied card name must not be empty.", nameof(cardName));
        }

        OwnerPlayerId = ownerPlayerId;
        CardId = cardId;
        CardName = cardName;
    }
}
