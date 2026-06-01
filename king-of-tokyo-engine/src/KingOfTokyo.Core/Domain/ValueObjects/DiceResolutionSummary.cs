namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record DiceResolutionSummary
{
    public int AttackCount { get; init; }
    public int EnergyCount { get; init; }
    public int HeartCount { get; init; }
    public int OneCount { get; init; }
    public int TwoCount { get; init; }
    public int ThreeCount { get; init; }
    public int VictoryPointsFromNumbers { get; init; }
}