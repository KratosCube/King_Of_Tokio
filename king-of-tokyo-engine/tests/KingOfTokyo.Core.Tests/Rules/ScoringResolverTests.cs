using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Scoring;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class ScoringResolverTests
{
    [Fact]
    public void ResolveVictoryPoints_Should_ReturnZero_WhenNoTripleExists()
    {
        var resolver = new ScoringResolver();
        var summary = new DiceResolutionSummary
        {
            OneCount = 2,
            TwoCount = 2,
            ThreeCount = 1,
            VictoryPointsFromNumbers = 0
        };

        var result = resolver.ResolveVictoryPoints(summary);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ResolveVictoryPoints_Should_ReturnOne_ForThreeOnes()
    {
        var resolver = new ScoringResolver();
        var summary = new DiceResolutionSummary
        {
            OneCount = 3,
            VictoryPointsFromNumbers = 1
        };

        var result = resolver.ResolveVictoryPoints(summary);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ResolveVictoryPoints_Should_ReturnThree_ForFourTwos()
    {
        var resolver = new ScoringResolver();
        var summary = new DiceResolutionSummary
        {
            TwoCount = 4,
            VictoryPointsFromNumbers = 3
        };

        var result = resolver.ResolveVictoryPoints(summary);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ResolveVictoryPoints_Should_ReturnFive_ForFiveThrees()
    {
        var resolver = new ScoringResolver();
        var summary = new DiceResolutionSummary
        {
            ThreeCount = 5,
            VictoryPointsFromNumbers = 5
        };

        var result = resolver.ResolveVictoryPoints(summary);

        Assert.Equal(5, result);
    }
}