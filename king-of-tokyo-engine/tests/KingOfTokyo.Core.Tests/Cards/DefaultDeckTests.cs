using System.Reflection;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Cards;

public sealed class DefaultDeckTests
{
    [Fact]
    public void DefaultDeck_Should_ContainEveryKnownCardId()
    {
        var knownCardIds = GetKnownCardIds();
        var defaultDeckCardIds = GetDefaultDeckCardIds();

        foreach (var knownCardId in knownCardIds)
        {
            Assert.Contains(knownCardId, defaultDeckCardIds);
        }
    }

    [Fact]
    public void DefaultDeck_Should_NotContainUnknownCardIds()
    {
        var knownCardIds = GetKnownCardIds();
        var defaultDeckCardIds = GetDefaultDeckCardIds();

        foreach (var deckCardId in defaultDeckCardIds)
        {
            Assert.Contains(deckCardId, knownCardIds);
        }
    }

    [Fact]
    public void DefaultDeck_Should_IncludeMimicAsKeepCard()
    {
        var mimic = GetDefaultDeckCards().Single(card => card.CardId == KnownCardIds.Mimic);

        Assert.Equal("Mimic", mimic.Name);
        Assert.Equal(8, mimic.Cost);
        Assert.Equal(MarketCardType.Keep, mimic.CardType);
        Assert.Contains("copy", mimic.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultDeck_Should_InitializeMarketWithThreeFaceUpCards()
    {
        var gameState = CreateGameState();
        var marketSetupService = new MarketSetupService();

        marketSetupService.InitializeMarket(gameState);

        Assert.Equal(MarketState.FaceUpSlotCount, gameState.Market.FaceUpCards.Count);
        Assert.All(gameState.Market.FaceUpCards, Assert.NotNull);
        Assert.True(gameState.Market.DrawPileCount > 0);
        Assert.Equal(0, gameState.Market.DiscardPileCount);
    }

    [Fact]
    public void DefaultDeck_Should_ShuffleMarketOrder_WhenInitializedWithSeededRandom()
    {
        var gameState = CreateGameState();
        var marketSetupService = new MarketSetupService(random: new Random(42));
        var unshuffledCardIds = MarketSetupService.BuildDefaultDeck()
            .Select(card => card.CardId)
            .ToArray();

        marketSetupService.InitializeMarket(gameState);

        var initializedCardIds = gameState.Market.FaceUpCards
            .Where(card => card is not null)
            .Select(card => card!.CardId)
            .Concat(gameState.Market.DrawPile.Select(card => card.CardId))
            .ToArray();

        Assert.Equal(unshuffledCardIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(), initializedCardIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.NotEqual(unshuffledCardIds.Take(10), initializedCardIds.Take(10));
    }

    private static IReadOnlySet<string> GetKnownCardIds()
    {
        return typeof(KnownCardIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> GetDefaultDeckCardIds()
    {
        return GetDefaultDeckCards()
            .Select(card => card.CardId)
            .ToArray();
    }

    private static IReadOnlyList<MarketCardState> GetDefaultDeckCards()
    {
        var gameState = CreateGameState();
        var marketSetupService = new MarketSetupService(shuffleDeck: false);

        marketSetupService.InitializeMarket(gameState);

        return gameState.Market.FaceUpCards
            .Where(card => card is not null)
            .Select(card => card!)
            .Concat(gameState.Market.DrawPile)
            .ToArray();
    }

    private static GameState CreateGameState()
    {
        var players = new[]
        {
            new PlayerState(0, "Monster 1"),
            new PlayerState(1, "Monster 2"),
            new PlayerState(2, "Monster 3"),
            new PlayerState(3, "Monster 4")
        };

        return new GameState(players, new GameOptions(players.Length));
    }
}
