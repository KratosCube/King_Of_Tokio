using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Domain.Entities;

public sealed class PlayerState
{
    private readonly List<MarketCardState> _keepCards = new();

    public int PlayerId { get; }
    public string MonsterName { get; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int VictoryPoints { get; private set; }
    public int Energy { get; private set; }
    public TokyoSlot TokyoSlot { get; private set; }
    public PlayerStatusState Status { get; } = new();
    public bool IsAlive => Health > 0;
    public IReadOnlyList<MarketCardState> KeepCards => _keepCards;

    public PlayerState(int playerId, string monsterName, int initialHealth = GameOptions.DefaultInitialHealth)
    {
        if (playerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "Player id must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(monsterName))
        {
            throw new ArgumentException("Monster name must not be empty.", nameof(monsterName));
        }

        if (initialHealth is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(initialHealth), "Initial health must be between 1 and 50.");
        }

        PlayerId = playerId;
        MonsterName = monsterName;
        Health = initialHealth;
        MaxHealth = initialHealth;
        VictoryPoints = 0;
        Energy = 0;
        TokyoSlot = TokyoSlot.None;
    }

    public void GainVictoryPoints(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        VictoryPoints += amount;
    }

    public void GainEnergy(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Energy += amount;
    }

    public void SpendEnergy(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (amount > Energy)
        {
            throw new InvalidOperationException("Cannot spend more energy than the player has.");
        }

        Energy -= amount;
    }

    public void TakeDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Health = Math.Max(0, Health - amount);
    }

    public void Heal(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Health = Math.Min(MaxHealth, Health + amount);
    }

    public void SetTokyoSlot(TokyoSlot slot)
    {
        TokyoSlot = slot;
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        MaxHealth += amount;
        Health = Math.Min(Health, MaxHealth);
    }

    public void DecreaseMaxHealth(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        MaxHealth = Math.Max(1, MaxHealth - amount);
        Health = Math.Min(Health, MaxHealth);
    }

    public void AddKeepCard(MarketCardState card)
    {
        ArgumentNullException.ThrowIfNull(card);
        _keepCards.Add(card);
    }

    public MarketCardState RemoveKeepCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        var card = _keepCards.FirstOrDefault(c => c.CardId == cardId);
        if (card is null)
        {
            throw new InvalidOperationException("Player does not own this keep card.");
        }

        _keepCards.Remove(card);
        return card;
    }

    public bool HasKeepCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        return _keepCards.Any(card => card.CardId == cardId);
    }
}
