using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using Xunit;

namespace KingOfTokyo.Core.Tests.Domain;

public sealed class MarketCardStateTests
{
    [Fact]
    public void Constructor_Should_InitializeCardLocalState()
    {
        var card = new MarketCardState(
            "card-test",
            "Test Card",
            "Test description.",
            3,
            MarketCardType.Keep,
            counters: 2,
            storedEnergy: 4);

        Assert.Equal(2, card.Counters);
        Assert.Equal(4, card.StoredEnergy);
    }

    [Fact]
    public void AddAndSpendCounters_Should_UpdateCounters()
    {
        var card = CreateKeepCard();

        card.AddCounters(3);
        card.SpendCounters(1);

        Assert.Equal(2, card.Counters);
    }

    [Fact]
    public void SpendCounters_Should_Fail_WhenAmountExceedsCounters()
    {
        var card = CreateKeepCard();

        var ex = Assert.Throws<InvalidOperationException>(() => card.SpendCounters(1));
        Assert.Equal("Cannot spend more counters than the card has.", ex.Message);
    }

    [Fact]
    public void AddAndSpendStoredEnergy_Should_UpdateStoredEnergy()
    {
        var card = CreateKeepCard();

        card.AddStoredEnergy(5);
        card.SpendStoredEnergy(2);

        Assert.Equal(3, card.StoredEnergy);
    }

    [Fact]
    public void SpendStoredEnergy_Should_Fail_WhenAmountExceedsStoredEnergy()
    {
        var card = CreateKeepCard();

        var ex = Assert.Throws<InvalidOperationException>(() => card.SpendStoredEnergy(1));
        Assert.Equal("Cannot spend more stored energy than the card has.", ex.Message);
    }

    private static MarketCardState CreateKeepCard()
    {
        return new MarketCardState(
            "card-test",
            "Test Card",
            "Test description.",
            3,
            MarketCardType.Keep);
    }
}
