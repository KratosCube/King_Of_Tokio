using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Api.Lobbies;
using KingOfTokyo.Core.Domain.Enums;
using Xunit;

namespace KingOfTokyo.Api.Tests.Lobbies;

public sealed class LobbyToGameSessionFlowTests
{
    [Fact]
    public void StartLobbyFlow_Should_CreateGameSessionFromLobbySeatsAndOptions()
    {
        var lobbyStore = new InMemoryLobbyStore();
        var gameSessionStore = new InMemoryGameSessionStore();
        var createdLobby = lobbyStore.CreateLobby(new CreateLobbyRequest(
            "Kaiju night",
            MaxPlayers: 4,
            IsPublic: true,
            HostDisplayName: "Host",
            InitialHealth: 15,
            TargetVictoryPoints: 30));
        lobbyStore.TryJoinLobby(createdLobby.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joined, out _);
        lobbyStore.TrySetReady(createdLobby.Lobby.LobbyId, new SetLobbyReadyRequest(joined!.PlayerToken, IsReady: true), out _, out _);

        var prepared = lobbyStore.TryPrepareStart(
            createdLobby.Lobby.LobbyId,
            new StartLobbyRequest(createdLobby.PlayerToken),
            out var preparation,
            out var prepareError);
        var game = gameSessionStore.CreateGame(preparation!.GameRequest);
        var attached = lobbyStore.TryAttachGame(createdLobby.Lobby.LobbyId, game.GameId, out var startedLobby, out var attachError);

        Assert.True(prepared);
        Assert.Null(prepareError);
        Assert.NotNull(preparation);
        Assert.True(attached);
        Assert.Null(attachError);
        Assert.NotNull(startedLobby);
        Assert.Equal(LobbyStatus.Started, startedLobby!.Status);
        Assert.Equal(game.GameId, startedLobby.GameId);
        Assert.Equal(GameStatus.Setup, game.Status);
        Assert.Equal(2, game.Players.Count);
        Assert.Equal("Host", game.Players[0].MonsterName);
        Assert.Equal("Guest", game.Players[1].MonsterName);
        Assert.All(game.Players, player =>
        {
            Assert.Equal(15, player.Health);
            Assert.Equal(15, player.MaxHealth);
        });
        gameSessionStore.TryExecute(game.GameId, (engine, state) =>
        {
            Assert.Equal(30, state.Options.TargetVictoryPoints);
            return KingOfTokyo.Core.Engine.CommandResult.Successful(state);
        }, out var commandResult);
        Assert.NotNull(commandResult);
        Assert.True(commandResult!.Success, commandResult.Error);
    }
}
