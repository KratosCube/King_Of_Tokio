using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record DamagePacket
{
    public required int SourcePlayerId { get; init; }
    public required int TargetPlayerId { get; init; }
    public required int Amount { get; init; }
    public required DamageKind DamageKind { get; init; }
    public required bool CountsAsAttack { get; init; }
    public required bool AllowsTokyoLeave { get; init; }
}