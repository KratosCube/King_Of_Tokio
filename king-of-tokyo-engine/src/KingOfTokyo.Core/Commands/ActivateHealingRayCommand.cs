using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateHealingRayCommand : CommandBase
{
    public int TargetPlayerId { get; }
    public int HealingAmount { get; }

    public ActivateHealingRayCommand(int targetPlayerId, int healingAmount, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (targetPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPlayerId));
        }

        if (healingAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(healingAmount));
        }

        TargetPlayerId = targetPlayerId;
        HealingAmount = healingAmount;
    }
}
