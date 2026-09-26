using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class RerollBackgroundDwellerThreesCommand : CommandBase
{
    public IReadOnlyList<int> DiceIndexes { get; }

    public RerollBackgroundDwellerThreesCommand(IEnumerable<int> diceIndexes, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        ArgumentNullException.ThrowIfNull(diceIndexes);
        DiceIndexes = diceIndexes.Distinct().ToArray();
    }
}
