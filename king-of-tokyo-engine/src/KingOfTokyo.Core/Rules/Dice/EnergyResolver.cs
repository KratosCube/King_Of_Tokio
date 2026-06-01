using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Dice;

public sealed class EnergyResolver
{
    public int ResolveEnergy(DiceResolutionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.EnergyCount;
    }
}