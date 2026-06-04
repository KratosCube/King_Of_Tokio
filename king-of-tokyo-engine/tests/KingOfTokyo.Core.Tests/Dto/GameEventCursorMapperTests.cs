using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
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
        Assert.Equal(0, cursor.FromVersionExclusive);
        Assert.Equal(gameState.Version, cursor.CurrentVersion);
        Assert.Single(cursor.Events);
        Assert.Equal(1, cursor.Events[0].Version);
        Assert.IsType<TurnStartedEvent>(cursor.Events[0].Event);
    }

    [Fact]
    public void MapEventsSince_Should_ReturnOnlyEventsAfterCursor()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        var afterBeginTurnVersion = gameState.Version;
        engine.Execute(gameState, new RollDiceCommand(0));

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, afterBeginTurnVersion);

        Assert.Equal(afterBeginTurnVersion, cursor.FromVersionExclusive);
        Assert.Equal(gameState.Version, cursor.CurrentVersion);
        Assert.Single(cursor.Events);
        Assert.Equal(afterBeginTurnVersion + 1, cursor.Events[0].Version);
        Assert.IsType<DiceRolledEvent>(cursor.Events[0].Event);
    }

    [Fact]
    public void MapEventsSince_Should_ReturnEmptyEvents_WhenCursorEqualsCurrentVersion()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var cursor = GameEventCursorMapper.MapEventsSince(gameState, gameState.Version);

        Assert.Equal(gameState.Version, cursor.FromVersionExclusive);
        Assert.Equal(gameState.Version, cursor.CurrentVersion);
        Assert.Empty(cursor.Events);
    }

    [Fact]
    public void MapEventsSince_Should_RejectCursorAheadOfCurrentVersion()
    {
        var gameState = CreateGameState(2);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameEventCursorMapper.MapEventsSince(gameState, gameState.Version + 1));

        Assert.Equal("Requested event cursor is ahead of the current game version.", exception.Message);
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
