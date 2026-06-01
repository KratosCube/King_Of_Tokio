using Xunit;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Tests.Domain;

public sealed class GameStateTests
{
    [Fact]
    public void Constructor_Should_EnableBay_ForFivePlayers()
    {
        var players = CreatePlayers(5);
        var options = new GameOptions(5);

        var gameState = new GameState(players, options);

        Assert.True(gameState.Tokyo.BayEnabled);
    }

    [Fact]
    public void Constructor_Should_DisableBay_ForFourPlayers()
    {
        var players = CreatePlayers(4);
        var options = new GameOptions(4);

        var gameState = new GameState(players, options);

        Assert.False(gameState.Tokyo.BayEnabled);
    }

    [Fact]
    public void StartGame_Should_ChangeStatusToRunning()
    {
        var gameState = CreateGameState(4);

        gameState.StartGame();

        Assert.Equal(GameStatus.Running, gameState.Status);
    }

    [Fact]
    public void StartTurnForCurrentPlayer_Should_CreateTurnState()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();

        gameState.StartTurnForCurrentPlayer();

        Assert.NotNull(gameState.CurrentTurn);
        Assert.Equal(0, gameState.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.TurnStart, gameState.CurrentTurn.Phase);
    }

    [Fact]
    public void StartTurnForCurrentPlayer_Should_SetStartedTurnInTokyoFlag_WhenPlayerIsInTokyo()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();

        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.SetTokyoSlot(TokyoSlot.City);

        gameState.StartTurnForCurrentPlayer();

        Assert.True(gameState.CurrentTurn!.Flags.StartedTurnInTokyo);
    }

    [Fact]
    public void AdvanceToNextAlivePlayer_Should_SkipDeadPlayers()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();

        gameState.GetPlayerById(1).TakeDamage(10);
        gameState.AdvanceToNextAlivePlayer();

        Assert.Equal(2, gameState.GetCurrentPlayer().PlayerId);
    }

    [Fact]
    public void FinishGame_Should_SetStatusAndWinner()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.FinishGame(WinnerInfo.Winner(2, "Reached 20 victory points."));

        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.Equal(2, gameState.WinnerInfo!.WinnerPlayerId);
        Assert.Equal(TurnPhase.Finished, gameState.CurrentTurn!.Phase);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = CreatePlayers(playerCount);
        var options = new GameOptions(playerCount);
        return new GameState(players, options);
    }

    private static IReadOnlyList<PlayerState> CreatePlayers(int count)
    {
        var players = new List<PlayerState>();

        for (var i = 0; i < count; i++)
        {
            players.Add(new PlayerState(i, $"Monster {i + 1}"));
        }

        return players;
    }
}