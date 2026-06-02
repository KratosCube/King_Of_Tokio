namespace KingOfTokyo.Core.Commands;

public sealed class ActivateHealingRayCommand
{
    public int? ActorPlayerId { get; }
    public int TargetPlayerId { get; }
    public int HealingAmount { get; }

    public ActivateHealingRayCommand(int targetPlayerId, int healingAmount, int? actorPlayerId = null)
    {
        if (targetPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPlayerId));
        }

        if (healingAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(healingAmount));
        }

        ActorPlayerId = actorPlayerId;
        TargetPlayerId = targetPlayerId;
        HealingAmount = healingAmount;
    }
}
