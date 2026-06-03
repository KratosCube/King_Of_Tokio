using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class VictoryEliminationTimingFlowTests
{
    [Fact]
    public void EndTurn_Should_NotEndGame_WhenCurrentPlayerReachedTwentyButFallsAndMultiplePlayersRemain()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.GainVictoryPoints(20);
        currentPlayer.TakeDamage(9);
        currentPlayer.Status.AddPoisonTokens(1);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.False(currentPlayer.IsAlive);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Null(gameState.WinnerInfo);
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent fallen &&
                                             fallen.EliminatedPlayerId == currentPlayer.PlayerId &&
                                             fallen.Reason == "Poison tokens.");
        Assert.DoesNotContain(result.NewEvents, e => e is GameEndedEvent);
    }

    [Fact]
    public void EndTurn_Should_AwardLastMonsterStandingToOtherPlayer_WhenTwentyPointCurrentPlayerFalls()
    {
        var gameState = CreateGameState(2);
        var currentPlayer = gameState.GetCurrentPlayer();
        var survivingPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainVictoryPoints(20);
        currentPlayer.TakeDamage(9);
        currentPlayer.Status.AddPoisonTokens(1);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.False(currentPlayer.IsAlive);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(survivingPlayer.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Last monster standing.", gameState.WinnerInfo.Reason);
        Assert.Contains(result.NewEvents, e => e is GameEndedEvent ended &&
                                             ended.WinnerPlayerId == survivingPlayer.PlayerId &&
                                             ended.Reason == "Last monster standing.");
    }

    [Fact]
    public void EndTurn_Should_FinishWithNoWinner_WhenTwentyPointCurrentPlayerFallsAndNoPlayersRemain()
    {
        var gameState = CreateGameState(2);
        var currentPlayer = gameState.GetCurrentPlayer();
        var otherPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainVictoryPoints(20);
        currentPlayer.TakeDamage(9);
        currentPlayer.Status.AddPoisonTokens(1);
        otherPlayer.TakeDamage(10);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.False(currentPlayer.IsAlive);
        Assert.False(otherPlayer.IsAlive);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.False(gameState.WinnerInfo!.HasWinner);
        Assert.Null(gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("All monsters were eliminated.", gameState.WinnerInfo.Reason);
        Assert.Contains(result.NewEvents, e => e is GameEndedEvent ended &&
                                             ended.WinnerPlayerId is null &&
                                             ended.Reason == "All monsters were eliminated.");
    }

    [Fact]
    public void EndTurn_Should_EndGame_WhenCurrentPlayerReachesTwentyFromEndTurnScoringAndSurvives()
    {
        var gameState = CreateGameState(2);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.GainVictoryPoints(19);
        currentPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Herbivore, "Herbivore", 5));
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.True(currentPlayer.IsAlive);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(currentPlayer.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", gameState.WinnerInfo.Reason);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == currentPlayer.PlayerId &&
                                             gained.Amount == 1 &&
                                             gained.Reason == "Keep card: Herbivore.");
        Assert.Contains(result.NewEvents, e => e is GameEndedEvent ended &&
                                             ended.WinnerPlayerId == currentPlayer.PlayerId &&
                                             ended.Reason == "Reached 20 victory points.");
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
    }
}
