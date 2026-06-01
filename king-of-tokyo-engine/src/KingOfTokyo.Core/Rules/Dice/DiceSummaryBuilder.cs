using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Dice;

public sealed class DiceSummaryBuilder
{
    public DiceResolutionSummary Build(DicePoolState dicePool)
    {
        ArgumentNullException.ThrowIfNull(dicePool);

        var faces = dicePool.Dice.Select(d => d.CurrentFace).ToArray();

        var oneCount = faces.Count(f => f == DieFace.One);
        var twoCount = faces.Count(f => f == DieFace.Two);
        var threeCount = faces.Count(f => f == DieFace.Three);
        var energyCount = faces.Count(f => f == DieFace.Energy);
        var attackCount = faces.Count(f => f == DieFace.Attack);
        var heartCount = faces.Count(f => f == DieFace.Heart);

        return new DiceResolutionSummary
        {
            OneCount = oneCount,
            TwoCount = twoCount,
            ThreeCount = threeCount,
            EnergyCount = energyCount,
            AttackCount = attackCount,
            HeartCount = heartCount,
            VictoryPointsFromNumbers = CalculateVictoryPointsFromNumbers(oneCount, twoCount, threeCount)
        };
    }

    private static int CalculateVictoryPointsFromNumbers(int oneCount, int twoCount, int threeCount)
    {
        var total = 0;

        total += ScoreNumberSet(oneCount, 1);
        total += ScoreNumberSet(twoCount, 2);
        total += ScoreNumberSet(threeCount, 3);

        return total;
    }

    private static int ScoreNumberSet(int count, int faceValue)
    {
        if (count < 3)
        {
            return 0;
        }

        return faceValue + (count - 3);
    }
}