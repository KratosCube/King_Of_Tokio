using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Decisions;

public sealed class PeekTopDeckCardDecisionData
{
    public string CardId { get; init; } = string.Empty;
    public string CardName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int BaseCost { get; init; }
    public int EffectiveCost { get; init; }
    public MarketCardType CardType { get; init; }
}