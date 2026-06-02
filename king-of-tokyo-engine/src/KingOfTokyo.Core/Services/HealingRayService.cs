using KingOfTokyo.Core.Domain.Entities;

namespace KingOfTokyo.Core.Services;

public sealed class HealingRayService
{
    public HealingRayResult HealOtherPlayer(PlayerState healer, PlayerState target, int requestedHealing)
    {
        ArgumentNullException.ThrowIfNull(healer);
        ArgumentNullException.ThrowIfNull(target);

        if (requestedHealing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedHealing));
        }

        if (healer.PlayerId == target.PlayerId)
        {
            throw new InvalidOperationException("Healing Ray can only heal other players.");
        }

        if (!healer.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot use Healing Ray.");
        }

        if (!target.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot be healed by Healing Ray.");
        }

        if (requestedHealing == 0)
        {
            return new HealingRayResult(0, 0);
        }

        var healthBefore = target.Health;
        target.Heal(requestedHealing);
        var actualHealing = target.Health - healthBefore;

        if (actualHealing <= 0)
        {
            return new HealingRayResult(0, 0);
        }

        var payment = Math.Min(target.Energy, actualHealing * 2);
        if (payment > 0)
        {
            target.SpendEnergy(payment);
            healer.GainEnergy(payment);
        }

        return new HealingRayResult(actualHealing, payment);
    }
}

public sealed record HealingRayResult(int HealedAmount, int EnergyPaid);
