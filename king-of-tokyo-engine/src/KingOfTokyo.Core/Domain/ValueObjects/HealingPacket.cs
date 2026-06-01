namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record HealingPacket
{
    public required int SourcePlayerId { get; init; }
    public required int TargetPlayerId { get; init; }
    public required int Amount { get; init; }
    public required bool AllowedInTokyo { get; init; }
}