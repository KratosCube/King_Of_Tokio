using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Healing;

public sealed class HealingResolver
{
    public int ResolveHealing(PlayerState player, DiceResolutionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(summary);

        if (!player.IsAlive)
        {
            return 0;
        }

        if (player.TokyoSlot != TokyoSlot.None)
        {
            return 0;
        }

        if (summary.HeartCount <= 0)
        {
            return 0;
        }

        var missingHealth = player.MaxHealth - player.Health;
        if (missingHealth <= 0)
        {
            return 0;
        }

        return Math.Min(summary.HeartCount, missingHealth);
    }
}