using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class SetMimicTargetCommand : CommandBase
{
    public int TargetOwnerPlayerId { get; }
    public string TargetCardId { get; }

    public SetMimicTargetCommand(int targetOwnerPlayerId, string targetCardId, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (targetOwnerPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetOwnerPlayerId));
        }

        if (string.IsNullOrWhiteSpace(targetCardId))
        {
            throw new ArgumentException("Target card id must not be empty.", nameof(targetCardId));
        }

        TargetOwnerPlayerId = targetOwnerPlayerId;
        TargetCardId = targetCardId;
    }
}
