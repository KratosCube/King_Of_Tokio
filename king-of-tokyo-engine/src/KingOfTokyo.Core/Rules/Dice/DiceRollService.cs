using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

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
        RollAll(dicePool, player: null);
    }

    public void RollAll(DicePoolState dicePool, PlayerState? player)
    {
        ArgumentNullException.ThrowIfNull(dicePool);

        foreach (var die in dicePool.Dice)
        {
            die.SetFace(_randomSource.RollDieFace());
        }

        RerollBackgroundDwellerThreesIfNeeded(dicePool, player);
    }

    public void RerollSelected(DicePoolState dicePool, IReadOnlyCollection<int> diceIndexesToReroll)
    {
        RerollSelected(dicePool, diceIndexesToReroll, player: null);
    }

    public void RerollSelected(DicePoolState dicePool, IReadOnlyCollection<int> diceIndexesToReroll, PlayerState? player)
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

        RerollBackgroundDwellerThreesIfNeeded(dicePool, player);
    }

    private void RerollBackgroundDwellerThreesIfNeeded(DicePoolState dicePool, PlayerState? player)
    {
        if (player is null || !player.HasKeepCard(KnownCardIds.BackgroundDweller))
        {
            return;
        }

        while (true)
        {
            var threeIndexes = dicePool.Dice
                .Where(die => die.CurrentFace == DieFace.Three)
                .Select(die => die.Index)
                .ToArray();

            if (threeIndexes.Length == 0)
            {
                return;
            }

            foreach (var index in threeIndexes)
            {
                dicePool.SetFace(index, _randomSource.RollDieFace());
            }
        }
    }
}
