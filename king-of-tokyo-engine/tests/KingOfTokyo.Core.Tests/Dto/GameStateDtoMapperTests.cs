using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Dto;

public sealed class GameStateDtoMapperTests
{
    [Fact]
    public void ToDto_Should_MapSetupGameState()
    {
        var gameId = Guid.NewGuid();
        var gameState = CreateGameState(4, gameId);

        var dto = gameState.ToDto();

        Assert.Equal(gameId, dto.GameId);
        Assert.Equal(0, dto.Version);
        Assert.Equal(GameStatus.Setup, dto.Status);
        Assert.Equal(0, dto.CurrentPlayerIndex);
        Assert.Null(dto.WinnerPlayerId);
        Assert.Null(dto.WinnerReason);
        Assert.Equal(4, dto.Players.Count);
        Assert.False(dto.Tokyo.BayEnabled);
        Assert.True(dto.Tokyo.IsEmpty);
        Assert.Equal(3, dto.Market.FaceUpCards.Count);
        Assert.Null(dto.CurrentTurn);
        Assert.Null(dto.PendingDecision);
    }

    [Fact]
    public void ToDto_Should_MapRunningGameStateAfterBeginTurn()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        gameEngine.Execute(gameState, new InitializeGameCommand());
        var result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        var dto = gameState.ToDto();

        Assert.True(result.Success);
        Assert.Equal(2, dto.Version);
        Assert.Equal(GameStatus.Running, dto.Status);
        Assert.NotNull(dto.CurrentTurn);
        Assert.Equal(0, dto.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, dto.CurrentTurn.Phase);
        Assert.Equal(6, dto.CurrentTurn.DiceCount);
        Assert.Equal(6, dto.CurrentTurn.Dice.Count);
        Assert.Single(gameState.EventLog);
    }

    [Fact]
    public void ToDto_Should_MapPendingDecision()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        gameEngine.Execute(gameState, new InitializeGameCommand());
        gameEngine.Execute(gameState, new BeginTurnCommand(0));
        var result = gameEngine.Execute(gameState, new RollDiceCommand(0));

        var dto = gameState.ToDto();

        Assert.True(result.Success);
        Assert.NotNull(dto.PendingDecision);
        Assert.Equal(Decisions.DecisionType.SelectDiceToReroll, dto.PendingDecision!.DecisionType);
        Assert.Equal(0, dto.PendingDecision.PlayerId);
    }

    [Fact]
    public void ToDto_Should_MapPlayerKeepCards()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.ExtraHead,
            "Extra Head",
            "You have 1 extra die.",
            7,
            MarketCardType.Keep));

        var dto = gameState.ToDto();

        Assert.Single(dto.Players[0].KeepCards);
        Assert.Equal(KnownCardIds.ExtraHead, dto.Players[0].KeepCards[0].CardId);
    }

    [Fact]
    public void ToDto_Should_MapPlayerStatusTokens()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        player.Status.AddPoisonTokens(2);
        player.Status.AddShrinkTokens(1);

        var dto = gameState.ToDto();

        Assert.Equal(2, dto.Players[0].Status.PoisonTokens);
        Assert.Equal(1, dto.Players[0].Status.ShrinkTokens);
    }

    private static GameState CreateGameState(int playerCount, Guid? gameId = null)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();
        var options = new GameOptions(playerCount);
        return new GameState(players, options, gameId);
    }
}
