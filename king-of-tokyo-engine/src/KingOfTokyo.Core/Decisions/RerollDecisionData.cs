using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Decisions;

public sealed record RerollDecisionData
{
    public required IReadOnlyList<int> CurrentLockedDiceIndexes { get; init; }
    public required IReadOnlyList<DieFace> CurrentFaces { get; init; }
    public required int RollCountUsed { get; init; }
    public required int MaxRolls { get; init; }
}