using KingOfTokyo.Core.Domain.Entities;
using Xunit;

namespace KingOfTokyo.Core.Tests.Domain;

public sealed class PlayerStatusStateTests
{
    [Fact]
    public void AddPoisonTokens_Should_IncreasePoisonTokens()
    {
        var status = new PlayerStatusState();

        status.AddPoisonTokens(2);

        Assert.Equal(2, status.PoisonTokens);
    }

    [Fact]
    public void RemovePoisonTokens_Should_NotGoBelowZero()
    {
        var status = new PlayerStatusState();
        status.AddPoisonTokens(1);

        status.RemovePoisonTokens(5);

        Assert.Equal(0, status.PoisonTokens);
    }

    [Fact]
    public void ClearPoisonTokens_Should_RemoveAllPoisonTokens()
    {
        var status = new PlayerStatusState();
        status.AddPoisonTokens(3);

        status.ClearPoisonTokens();

        Assert.Equal(0, status.PoisonTokens);
    }

    [Fact]
    public void AddShrinkTokens_Should_IncreaseShrinkTokens()
    {
        var status = new PlayerStatusState();

        status.AddShrinkTokens(2);

        Assert.Equal(2, status.ShrinkTokens);
    }

    [Fact]
    public void RemoveShrinkTokens_Should_NotGoBelowZero()
    {
        var status = new PlayerStatusState();
        status.AddShrinkTokens(1);

        status.RemoveShrinkTokens(5);

        Assert.Equal(0, status.ShrinkTokens);
    }

    [Fact]
    public void ClearShrinkTokens_Should_RemoveAllShrinkTokens()
    {
        var status = new PlayerStatusState();
        status.AddShrinkTokens(3);

        status.ClearShrinkTokens();

        Assert.Equal(0, status.ShrinkTokens);
    }
}
