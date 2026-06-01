namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record CardPurchaseEffect
{
    public static CardPurchaseEffect None { get; } = new();

    public int GainVictoryPoints { get; init; }
    public int GainEnergy { get; init; }
    public int Heal { get; init; }
    public int IncreaseMaxHealth { get; init; }
    public int DamageAllOthers { get; init; }
    public int DamageAllIncludingSelf { get; init; }
    public int DamageSelf { get; init; }

    // Každý jiný hráč utrpí N zranění za každé 2 energy, které má.
    public int DamageOthersPerTwoEnergy { get; init; }
}