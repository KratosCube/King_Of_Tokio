using Xunit;
using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Events;

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
    public void Constructor_Should_AssignGameIdAndStartAtVersionZero()
    {
        var gameId = Guid.NewGuid();
        var gameState = CreateGameState(4, gameId);

        Assert.Equal(gameId, gameState.GameId);
        Assert.Equal(0, gameState.Version);
        Assert.Empty(gameState.EventLog);
        Assert.Empty(gameState.ScheduledTurnPlayerIds);
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
    public void AdvanceToNextAlivePlayer_Should_UseScheduledExtraTurnBeforeNormalOrder()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();
        gameState.ScheduleExtraTurn(0);

        gameState.AdvanceToNextAlivePlayer();

        Assert.Equal(0, gameState.GetCurrentPlayer().PlayerId);
        Assert.Empty(gameState.ScheduledTurnPlayerIds);
    }

    [Fact]
    public void AdvanceToNextAlivePlayer_Should_SkipDeadScheduledPlayer()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();
        gameState.ScheduleExtraTurn(2);
        gameState.GetPlayerById(2).TakeDamage(10);

        gameState.AdvanceToNextAlivePlayer();

        Assert.Equal(1, gameState.GetCurrentPlayer().PlayerId);
        Assert.Empty(gameState.ScheduledTurnPlayerIds);
    }

    [Fact]
    public void ScheduleExtraTurn_Should_Fail_ForDeadPlayer()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();
        gameState.GetPlayerById(2).TakeDamage(10);

        var ex = Assert.Throws<InvalidOperationException>(() => gameState.ScheduleExtraTurn(2));

        Assert.Equal("Cannot schedule an extra turn for a dead player.", ex.Message);
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

    [Fact]
    public void CommandResultSuccessful_Should_IncrementVersionAndRecordEvents()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        var result = gameEngine.Execute(gameState, new InitializeGameCommand());

        Assert.True(result.Success);
        Assert.Equal(1, gameState.Version);
        Assert.Empty(gameState.EventLog);

        result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(2, gameState.Version);
        Assert.Single(gameState.EventLog);
        Assert.IsType<TurnStartedEvent>(gameState.EventLog[0]);
    }

    [Fact]
    public void CommandResultSuccessful_Should_RecordReturnedEventsInOrder()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        gameEngine.Execute(gameState, new InitializeGameCommand());

        var result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        Assert.True(result.Success);
        Assert.Single(result.NewEvents);
        Assert.Single(gameState.EventLog);
        Assert.Same(result.NewEvents[0], gameState.EventLog[0]);
    }

    [Fact]
    public void CommandResultSuccessful_Should_IncrementVersionOnlyOnce_WhenRecordingMultipleEvents()
    {
        var gameState = CreateGameState(4);
        var versionBefore = gameState.Version;
        GameEventBase[] eventsToRecord =
        {
            new TurnStartedEvent(0),
            new VictoryPointsGainedEvent(0, 2, "Test event.")
        };

        var result = CommandResult.Successful(gameState, eventsToRecord);

        Assert.True(result.Success);
        Assert.Equal(versionBefore + 1, gameState.Version);
        Assert.Equal(eventsToRecord, result.NewEvents);
        Assert.Equal(eventsToRecord, gameState.EventLog);
    }

    [Fact]
    public void CommandResultFailed_Should_NotIncrementVersionOrRecordEvents()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        var result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        Assert.False(result.Success);
        Assert.Equal(0, gameState.Version);
        Assert.Empty(gameState.EventLog);
    }

    [Fact]
    public void CommandResultFailed_Should_NotMutateVersionOrEventLog_AfterSuccessfulCommands()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();
        gameEngine.Execute(gameState, new InitializeGameCommand());
        gameEngine.Execute(gameState, new BeginTurnCommand(0));
        var versionBeforeFailure = gameState.Version;
        var eventLogCountBeforeFailure = gameState.EventLog.Count;

        var result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        Assert.False(result.Success);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventLogCountBeforeFailure, gameState.EventLog.Count);
    }

    private static GameState CreateGameState(int playerCount, Guid? gameId = null)
    {
        var players = CreatePlayers(playerCount);
        var options = new GameOptions(playerCount);
        return new GameState(players, options, gameId);
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
