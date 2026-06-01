namespace KingOfTokyo.Core.Decisions;

public sealed record TokyoLeaveDecisionContext
{
    public required int AttackerPlayerId { get; init; }
    public required int DefenderPlayerId { get; init; }
    public required int DamageTaken { get; init; }
}