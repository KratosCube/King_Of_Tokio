namespace KingOfTokyo.Core.Decisions;

public sealed record MarketCardRevealDecisionData
{
    public required int SlotIndex { get; init; }
    public required string CardId { get; init; }
    public required string CardName { get; init; }
    public required int Cost { get; init; }
    public required IReadOnlyList<int> EligiblePlayerIds { get; init; }
}
