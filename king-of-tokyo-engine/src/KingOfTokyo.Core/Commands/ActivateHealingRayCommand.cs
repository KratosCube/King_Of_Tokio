namespace KingOfTokyo.Core.Commands;

public sealed class ActivateHealingRayCommand : KingOfTokyo.Core.Abstractions.CommandBase
{
    public int TargetPlayerId { get; }
    public int HealingAmount { get; }

    public ActivateHealingRayCommand(int targetPlayerId, int healingAmount, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        TargetPlayerId = targetPlayerId;
        HealingAmount = healingAmount;
    }
}
