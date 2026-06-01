namespace KingOfTokyo.Core.Decisions;

public sealed record PendingDecision
{
    public required DecisionType DecisionType { get; init; }
    public required int PlayerId { get; init; }
    public object? Payload { get; init; }
}