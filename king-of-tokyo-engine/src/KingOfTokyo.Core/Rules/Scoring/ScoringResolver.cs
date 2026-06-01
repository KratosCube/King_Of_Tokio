using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Scoring;

public sealed class ScoringResolver
{
    public int ResolveVictoryPoints(DiceResolutionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.VictoryPointsFromNumbers;
    }
}