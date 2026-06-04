using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Victory;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class VictoryResolverTests
{
    [Fact]
    public void Resolve_Should_ReturnWinner_WhenCurrentPlayerHasTwentyPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetCurrentPlayer().GainVictoryPoints(20);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.Equal(0, result!.WinnerPlayerId);
    }

    [Fact]
    public void Resolve_Should_ReturnWinner_WhenCurrentPlayerReachesCustomTargetVictoryPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, targetVictoryPoints: 12));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();
        gameState.GetCurrentPlayer().GainVictoryPoints(12);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.True(result!.HasWinner);
        Assert.Equal(0, result.WinnerPlayerId);
        Assert.Equal("Reached 12 victory points.", result.Reason);
    }

    [Fact]
    public void Resolve_Should_NotReturnWinner_WhenPlayerIsBelowCustomTargetVictoryPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, targetVictoryPoints: 12));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();
        gameState.GetCurrentPlayer().GainVictoryPoints(11);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Should_ReturnWinner_WhenNonCurrentAlivePlayerHasTwentyPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetPlayerById(2).GainVictoryPoints(20);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.True(result!.HasWinner);
        Assert.Equal(2, result.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", result.Reason);
    }

    [Fact]
    public void Resolve_Should_ReturnWinner_WhenVictoryModeIsFirstToTwentyAndAlivePlayerHasTwentyPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, VictoryMode.FirstToTwentyPoints));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetPlayerById(2).GainVictoryPoints(20);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.True(result!.HasWinner);
        Assert.Equal(2, result.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", result.Reason);
    }

    [Fact]
    public void Resolve_Should_NotReturnLastMonsterStandingWinner_WhenVictoryModeIsFirstToTwenty()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, VictoryMode.FirstToTwentyPoints));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetPlayerById(1).TakeDamage(10);
        gameState.GetPlayerById(2).TakeDamage(10);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Should_NotReturnTwentyPointWinner_WhenVictoryModeIsLastMonsterStanding()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, VictoryMode.LastMonsterStanding));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetCurrentPlayer().GainVictoryPoints(20);
        gameState.GetPlayerById(2).GainVictoryPoints(20);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Should_ReturnLastMonsterStandingWinner_WhenVictoryModeIsLastMonsterStanding()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3, VictoryMode.LastMonsterStanding));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetPlayerById(1).TakeDamage(10);
        gameState.GetPlayerById(2).TakeDamage(10);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.True(result!.HasWinner);
        Assert.Equal(0, result.WinnerPlayerId);
        Assert.Equal("Last monster standing.", result.Reason);
    }

    [Fact]
    public void Resolve_Should_NotReturnTwentyPointWinner_WhenNonCurrentPlayerHasTwentyPointsButIsDead()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        var deadPlayer = gameState.GetPlayerById(2);
        deadPlayer.GainVictoryPoints(20);
        deadPlayer.TakeDamage(10);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Should_PrioritizeCurrentPlayer_WhenCurrentAndOtherAlivePlayersHaveTwentyPoints()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetCurrentPlayer().GainVictoryPoints(20);
        gameState.GetPlayerById(2).GainVictoryPoints(20);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.True(result!.HasWinner);
        Assert.Equal(0, result.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", result.Reason);
    }

    [Fact]
    public void Resolve_Should_ReturnWinner_WhenOnlyOnePlayerIsAlive()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        gameState.GetPlayerById(1).TakeDamage(10);
        gameState.GetPlayerById(2).TakeDamage(10);

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.Equal(0, result!.WinnerPlayerId);
    }

    [Fact]
    public void Resolve_Should_ReturnNoWinnerInfo_WhenAllPlayersAreDead()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        foreach (var player in gameState.Players)
        {
            player.TakeDamage(10);
        }

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.NotNull(result);
        Assert.False(result!.HasWinner);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_WhenNoVictoryConditionIsMet()
    {
        var players = CreatePlayers(3);
        var gameState = new GameState(players, new GameOptions(3));
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer();

        var resolver = new VictoryResolver();

        var result = resolver.Resolve(gameState);

        Assert.Null(result);
    }

    private static IReadOnlyList<PlayerState> CreatePlayers(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();
    }
}
