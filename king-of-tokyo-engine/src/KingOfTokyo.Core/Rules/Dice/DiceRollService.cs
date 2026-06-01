using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;

namespace KingOfTokyo.Core.Rules.Dice;

public sealed class DiceRollService
{
    private readonly IRandomSource _randomSource;

    public DiceRollService(IRandomSource randomSource)
    {
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

    public void RollAll(DicePoolState dicePool)
    {
        ArgumentNullException.ThrowIfNull(dicePool);

        foreach (var die in dicePool.Dice)
        {
            die.SetFace(_randomSource.RollDieFace());
        }
    }

    public void RerollSelected(DicePoolState dicePool, IReadOnlyCollection<int> diceIndexesToReroll)
    {
        ArgumentNullException.ThrowIfNull(dicePool);
        ArgumentNullException.ThrowIfNull(diceIndexesToReroll);

        if (diceIndexesToReroll.Count == 0)
        {
            throw new InvalidOperationException("At least one die must be selected for reroll.");
        }

        foreach (var index in diceIndexesToReroll)
        {
            if (index < 0 || index >= dicePool.Dice.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(diceIndexesToReroll), $"Invalid die index: {index}");
            }
        }

        foreach (var index in diceIndexesToReroll)
        {
            dicePool.SetFace(index, _randomSource.RollDieFace());
        }
    }
}