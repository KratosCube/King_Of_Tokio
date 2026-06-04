using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using Xunit;

namespace KingOfTokyo.Core.Tests.Dto;

public sealed class GameEventCursorMapperTests
{
    [Fact]
    public void MapEventsSince_Should_ReturnAllLoggedEvents_WhenCursorIsZero()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, 0);

        Assert.Equal(gameState.GameId, cursor.GameId);
        Assert.Equal(0, cursor.FromEventSequenceExclusive);
        Assert.Equal(gameState.EventLog.Count, cursor.CurrentEventSequence);
        Assert.Equal(gameState.Version, cursor.CurrentGameVersion);
        Assert.Single(cursor.Events);
        Assert.Equal(1, cursor.Events[0].EventSequence);
        Assert.IsType<TurnStartedEvent>(cursor.Events[0].Event);
    }

    [Fact]
    public void MapEventsSince_Should_ReturnOnlyEventsAfterEventSequenceCursor()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        var afterBeginTurnEventSequence = gameState.EventLog.Count;
        engine.Execute(gameState, new RollDiceCommand(0));

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, afterBeginTurnEventSequence);

        Assert.Equal(afterBeginTurnEventSequence, cursor.FromEventSequenceExclusive);
        Assert.Equal(gameState.EventLog.Count, cursor.CurrentEventSequence);
        Assert.Equal(gameState.Version, cursor.CurrentGameVersion);
        Assert.Single(cursor.Events);
        Assert.Equal(afterBeginTurnEventSequence + 1, cursor.Events[0].EventSequence);
        Assert.IsType<DiceRolledEvent>(cursor.Events[0].Event);
    }

    [Fact]
    public void MapEventsSince_Should_ReturnEmptyEvents_WhenCursorEqualsCurrentEventSequence()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, gameState.EventLog.Count);

        Assert.Equal(gameState.EventLog.Count, cursor.FromEventSequenceExclusive);
        Assert.Equal(gameState.EventLog.Count, cursor.CurrentEventSequence);
        Assert.Equal(gameState.Version, cursor.CurrentGameVersion);
        Assert.Empty(cursor.Events);
    }

    [Fact]
    public void MapEventsSince_Should_TrackGameVersionSeparatelyFromEventSequence_WhenSuccessfulCommandHasNoEvents()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());

        Assert.Equal(1, gameState.Version);
        Assert.Empty(gameState.EventLog);

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, 0);

        Assert.Equal(0, cursor.FromEventSequenceExclusive);
        Assert.Equal(0, cursor.CurrentEventSequence);
        Assert.Equal(1, cursor.CurrentGameVersion);
        Assert.Empty(cursor.Events);
    }

    [Fact]
    public void MapEventsSince_Should_RejectCursorAheadOfCurrentEventSequence()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameEventCursorMapper.MapEventsSince(gameState, gameState.EventLog.Count + 1));

        Assert.Equal("Requested event cursor is ahead of the current event sequence.", exception.Message);
    }

    [Fact]
    public void MapEventsSince_Should_RejectNegativeCursor()
    {
        var gameState = CreateGameState(2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GameEventCursorMapper.MapEventsSince(gameState, -1));
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }
}
