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
        Assert.Equal(3, gameState.Market.FaceUpCards.Count(card => card is not null));
        Assert.Equal(36, gameState.Market.DrawPileCount);
        Assert.Equal(0, gameState.Market.DiscardPileCount);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }
}