using System.Text.Json;
using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;
using Xunit;

namespace KingOfTokyo.Api.Tests.GameSessions;

public sealed class InMemoryGameSessionStoreTests
{
    [Fact]
    public void CreateGame_Should_ReturnSnapshot()
    {
        var store = new InMemoryGameSessionStore();

        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }));

        Assert.NotEqual(Guid.Empty, created.GameId);
        Assert.Equal("Setup", created.Status.ToString());
        Assert.Equal(2, created.Players.Count);
    }

    [Fact]
    public void CreateGame_Should_AllowTwoPlayersForOnlineMvp()
    {
        var store = new InMemoryGameSessionStore();

        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }));

        Assert.Equal(2, created.Players.Count);
        Assert.False(created.Tokyo.BayEnabled);
    }

    [Fact]
    public void CreateGame_Should_UseCustomGameOptions_WhenProvided()
    {
        var store = new InMemoryGameSessionStore();

        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }, InitialHealth: 12, TargetVictoryPoints: 30));

        Assert.All(created.Players, player => Assert.Equal(12, player.Health));
        Assert.All(created.Players, player => Assert.Equal(12, player.MaxHealth));
    }

    [Fact]
    public void CreateGame_Should_RejectInvalidPlayerCount()
    {
        var store = new InMemoryGameSessionStore();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            store.CreateGame(new CreateGameRequest(new[] { "Solo" })));

        Assert.Equal("Player count must be between 2 and 6. (Parameter 'playerCount')", exception.Message);
    }

    [Fact]
    public void CreateGame_Should_RejectInvalidInitialHealth()
    {
        var store = new InMemoryGameSessionStore();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }, InitialHealth: 0)));

        Assert.Equal("Initial health must be between 1 and 50. (Parameter 'initialHealth')", exception.Message);
    }

    [Fact]
    public void CreateGame_Should_RejectInvalidTargetVictoryPoints()
    {
        var store = new InMemoryGameSessionStore();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }, TargetVictoryPoints: 0)));

        Assert.Equal("Target victory points must be between 1 and 100. (Parameter 'targetVictoryPoints')", exception.Message);
    }

    [Fact]
    public void TryGetSnapshot_Should_ReturnSnapshot_WhenGameExists()
    {
        var store = new InMemoryGameSessionStore();
        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }));

        var found = store.TryGetSnapshot(created.GameId, out var snapshot);

        Assert.True(found);
        Assert.NotNull(snapshot);
        Assert.Equal(created.GameId, snapshot!.GameId);
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
    public void TryExecute_Should_RunCommandAndUpdateSnapshot()
    {
        var store = new InMemoryGameSessionStore();
        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }));

        var executed = store.TryExecute(
            created.GameId,
            (engine, state) => engine.Execute(state, new InitializeGameCommand()),
            out var result);

        Assert.True(executed);
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Running", result.GameState.Status.ToString());
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
        var created = store.CreateGame(new CreateGameRequest(new[] { "Alpha", "Beta" }));
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
        AssertEventName("TurnStartedEvent", cursor.Events[0].Event);
    }

    [Fact]
    public void TryGetEvents_Should_ReturnFalse_WhenGameDoesNotExist()
    {
        var store = new InMemoryGameSessionStore();

        var found = store.TryGetEvents(Guid.NewGuid(), 0, out var cursor);

        Assert.False(found);
        Assert.Null(cursor);
    }

    private static void AssertEventName(string expectedEventName, JsonElement eventJson)
    {
        Assert.Equal(JsonValueKind.Object, eventJson.ValueKind);
        Assert.True(eventJson.TryGetProperty("eventName", out var eventName));
        Assert.Equal(expectedEventName, eventName.GetString());
    }
}
