using System.Reflection;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MarketInitializationFlowTests
{
    [Fact]
    public void InitializeGame_Should_SetupMarketWithThreeFaceUpCards()
    {
        var gameState = CreateGameState(4);
        var engine = new GameEngine();

        var result = engine.Execute(gameState, new InitializeGameCommand());

        Assert.True(result.Success);
        Assert.Equal(MarketState.FaceUpSlotCount, gameState.Market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(GetKnownCardCount() - MarketState.FaceUpSlotCount, gameState.Market.DrawPileCount);
        Assert.Equal(0, gameState.Market.DiscardPileCount);
    }

    private static int GetKnownCardCount()
    {
        return typeof(KnownCardIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Count(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string));
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }
}
