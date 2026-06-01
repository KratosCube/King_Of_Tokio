using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Healing;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class HealingResolverTests
{
    [Fact]
    public void ResolveHealing_Should_HealByHeartCount_WhenOutsideTokyo()
    {
        var player = new PlayerState(0, "Monster");
        player.TakeDamage(4);

        var summary = new DiceResolutionSummary
        {
            HeartCount = 3
        };

        var resolver = new HealingResolver();

        var result = resolver.ResolveHealing(player, summary);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ResolveHealing_Should_CapAtMissingHealth()
    {
        var player = new PlayerState(0, "Monster");
        player.TakeDamage(2);

        var summary = new DiceResolutionSummary
        {
            HeartCount = 5
        };

        var resolver = new HealingResolver();

        var result = resolver.ResolveHealing(player, summary);

        Assert.Equal(2, result);
    }

    [Fact]
    public void ResolveHealing_Should_ReturnZero_WhenPlayerIsInTokyo()
    {
        var player = new PlayerState(0, "Monster");
        player.TakeDamage(5);
        player.SetTokyoSlot(TokyoSlot.City);

        var summary = new DiceResolutionSummary
        {
            HeartCount = 3
        };

        var resolver = new HealingResolver();

        var result = resolver.ResolveHealing(player, summary);

        Assert.Equal(0, result);
    }
}