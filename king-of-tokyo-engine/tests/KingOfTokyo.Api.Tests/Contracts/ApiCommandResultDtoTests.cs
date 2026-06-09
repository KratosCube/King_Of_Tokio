using System.Text.Json;
using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Api.Tests.Contracts;

public sealed class ApiCommandResultDtoTests
{
    [Fact]
    public void From_Should_MapSuccessfulCommandResultWithSnapshotAndEventSequence()
    {
        var gameState = CreateGameState();
        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        var commandResult = engine.Execute(gameState, new BeginTurnCommand(0));

        var dto = ApiCommandResultDto.From(commandResult);

        Assert.True(dto.Success);
        Assert.Null(dto.Error);
        Assert.Equal(gameState.GameId, dto.GameState.GameId);
        Assert.Equal(gameState.Version, dto.GameState.Version);
        Assert.Equal(gameState.EventLog.Count, dto.CurrentEventSequence);
        Assert.Single(dto.NewEvents);
        AssertEventName("TurnStartedEvent", dto.NewEvents[0]);
    }

    [Fact]
    public void From_Should_MapFailedCommandResultWithoutAddingEvents()
    {
        var gameState = CreateGameState();
        var commandResult = CommandResult.Failed(gameState, "Invalid command.");

        var dto = ApiCommandResultDto.From(commandResult);

        Assert.False(dto.Success);
        Assert.Equal("Invalid command.", dto.Error);
        Assert.Equal(gameState.GameId, dto.GameState.GameId);
        Assert.Equal(gameState.Version, dto.GameState.Version);
        Assert.Equal(0, dto.CurrentEventSequence);
        Assert.Empty(dto.NewEvents);
    }

    [Fact]
    public void From_Should_KeepEventSequenceSeparateFromGameVersion()
    {
        var gameState = CreateGameState();
        var commandResult = CommandResult.Successful(gameState);

        var dto = ApiCommandResultDto.From(commandResult);

        Assert.True(dto.Success);
        Assert.Equal(1, dto.GameState.Version);
        Assert.Equal(0, dto.CurrentEventSequence);
        Assert.Empty(dto.NewEvents);
    }

    private static void AssertEventName(string expectedEventName, JsonElement eventJson)
    {
        Assert.Equal(JsonValueKind.Object, eventJson.ValueKind);
        Assert.True(eventJson.TryGetProperty("eventName", out var eventName));
        Assert.Equal(expectedEventName, eventName.GetString());
    }

    private static GameState CreateGameState()
    {
        var players = new[]
        {
            new PlayerState(0, "Alpha"),
            new PlayerState(1, "Beta")
        };

        return new GameState(players, new GameOptions(players.Length));
    }
}
