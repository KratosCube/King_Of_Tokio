using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class RerollDiceCommand : CommandBase
{
    public IReadOnlyList<int> DiceIndexesToReroll { get; }

    public RerollDiceCommand(IEnumerable<int> diceIndexesToReroll, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        ArgumentNullException.ThrowIfNull(diceIndexesToReroll);

        DiceIndexesToReroll = diceIndexesToReroll.Distinct().OrderBy(i => i).ToArray();
    }
}