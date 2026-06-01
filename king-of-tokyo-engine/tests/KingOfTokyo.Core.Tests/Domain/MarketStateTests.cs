using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using Xunit;

namespace KingOfTokyo.Core.Tests.Domain;

public sealed class MarketStateTests
{
    [Fact]
    public void Initialize_Should_FillUpToThreeFaceUpSlots()
    {
        var market = new MarketState();
        var deck = CreateDeck(8);

        market.Initialize(deck);

        Assert.Equal(3, market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(5, market.DrawPileCount);
        Assert.Equal(0, market.DiscardPileCount);
    }

    [Fact]
    public void Initialize_Should_HandleShortDeck()
    {
        var market = new MarketState();
        var deck = CreateDeck(2);

        market.Initialize(deck);

        Assert.Equal(2, market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(0, market.DrawPileCount);
    }

    [Fact]
    public void RemoveFaceUpCardAt_Should_RefillSlot_WhenDeckHasCards()
    {
        var market = new MarketState();
        var deck = CreateDeck(5);

        market.Initialize(deck);

        var removed = market.RemoveFaceUpCardAt(1);

        Assert.NotNull(removed);
        Assert.Equal(3, market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(1, market.DrawPileCount);
    }

    [Fact]
    public void RefreshAllFaceUpCards_Should_MoveCardsToDiscard_AndRefill()
    {
        var market = new MarketState();
        var deck = CreateDeck(6);

        market.Initialize(deck);
        market.RefreshAllFaceUpCards();

        Assert.Equal(3, market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(3, market.DiscardPileCount);
        Assert.Equal(0, market.DrawPileCount);
    }

    private static IReadOnlyList<MarketCardState> CreateDeck(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new MarketCardState(
                $"card-{i:000}",
                $"Card {i}",
                $"Description {i}",
                i % 6,
                i % 2 == 0 ? MarketCardType.Keep : MarketCardType.Discard))
            .ToArray();
    }
}