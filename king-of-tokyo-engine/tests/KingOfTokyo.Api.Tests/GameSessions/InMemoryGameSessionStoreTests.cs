using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Events;
using Xunit;

namespace KingOfTokyo.Api.Tests.GameSessions;

public sealed class InMemoryGameSessionStoreTests
{
    [Fact]
    public void CreateGame_Should_CreateSnapshotWithRequestedMonsterNames()
    {
        var store = new InMemoryGameSessionStore();

        var snapshot = store.CreateGame(new[] { "Alpha", "Beta" });

        Assert.NotEqual(Guid.Empty, snapshot.GameId);
        Assert.Equal(GameStatus.NotStarted, snapshot.Status);
        Assert.Equal(2, snapshot.Players.Count);
        Assert.Equal("Alpha", snapshot.Players[0].MonsterName);
        Assert.Equal("Beta", snapshot.Players[1].MonsterName);
        Assert.False(snapshot.Tokyo.BayEnabled);
    }

    [Fact]
    public void CreateGame_Should_UseDefaultMonsterName_WhenNameIsBlank()
    {
        var store = new InMemoryGameSessionStore();

        var snapshot = store.CreateGame(new[] { "", "  " });

        Assert.Equal("Monster 1", snapshot.Players[0].MonsterName);
        Assert.Equal("Monster 2", snapshot.Players[1].MonsterName);
    }

    [Fact]
    public void TryGetSnapshot_Should_ReturnCreatedGameSnapshot()
    {
        var store = new InMemoryGameSessionStore();
        var created = store.CreateGame(new[] { "Alpha", "Beta", "Gamma" });

        var found = store.TryGetSnapshot(created.GameId, out var snapshot);

        Assert.True(found);
        Assert.NotNull(snapshot);
        Assert.Equal(created.GameId, snapshot!.GameId);
        Assert.Equal(3, snapshot.Players.Count);
    }

    [Fact]
    public void TryGetSnapshot_Should_ReturnFalse_WhenGameDoesNotExist()
    {
        var store = new InMemoryGameSessionStore();

        var found = store.TryGetSnapshot(Guid.NewGuid(), out var snapshot);

        Assert.False(found);
        Assert.Null(snapshot);
    }

    [Fact]
    public void TryExecute_Should_RunCommandAndReturnUpdatedSnapshotAndEvents()
    {
        var store = new InMemoryGameSessionStore();
        var created = store.CreateGame(new[] { "Alpha", "Beta" });

        var executed = store.TryExecute(
            created.GameId,
            (engine, state) => engine.Execute(state, new InitializeGameCommand()),
            out var result);

        Assert.True(executed);
        Assert.NotNull(result);
        Assert.True(result!.Success, result.Error);
        Assert.Equal(GameStatus.Running, result.GameState.Status);
        Assert.Equal(1, result.GameState.Version);
        Assert.Empty(result.NewEvents);
        Assert.Equal(0, result.CurrentEventSequence);
    }

    [Fact]
    public void TryExecute_Should_ReturnFalse_WhenGameDoesNotExist()
    {
        var store = new InMemoryGameSessionStore();

        var executed = store.TryExecute(
            Guid.NewGuid(),
            (engine, state) => engine.Execute(state, new InitializeGameCommand()),
            out var result);

        Assert.False(executed);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetEvents_Should_ReturnEventCursorAfterCommands()
    {
        var store = new InMemoryGameSessionStore();
        var created = store.CreateGame(new[] { "Alpha", "Beta" });
        store.TryExecute(created.GameId, (engine, state) => engine.Execute(state, new InitializeGameCommand()), out _);
        store.TryExecute(created.GameId, (engine, state) => engine.Execute(state, new BeginTurnCommand(0)), out _);

        var found = store.TryGetEvents(created.GameId, 0, out var cursor);

        Assert.True(found);
        Assert.NotNull(cursor);
        Assert.Equal(created.GameId, cursor!.GameId);
        Assert.Equal(0, cursor.FromEventSequenceExclusive);
        Assert.Equal(1, cursor.CurrentEventSequence);
        Assert.Equal(2, cursor.CurrentGameVersion);
        Assert.Single(cursor.Events);
        Assert.Equal(1, cursor.Events[0].EventSequence);
        Assert.IsType<TurnStartedEvent>(cursor.Events[0].Event);
    }

    [Fact]
    public void TryGetEvents_Should_ReturnFalse_WhenGameDoesNotExist()
    {
        var store = new InMemoryGameSessionStore();

        var found = store.TryGetEvents(Guid.NewGuid(), 0, out var cursor);

        Assert.False(found);
        Assert.Null(cursor);
    }
}
