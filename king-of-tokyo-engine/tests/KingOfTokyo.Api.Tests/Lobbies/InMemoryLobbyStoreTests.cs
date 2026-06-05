using KingOfTokyo.Api.Lobbies;
using KingOfTokyo.Core.Domain.ValueObjects;
using Xunit;

namespace KingOfTokyo.Api.Tests.Lobbies;

public sealed class InMemoryLobbyStoreTests
{
    [Fact]
    public void CreateLobby_Should_CreateHostSeatAndDefaultGameOptions()
    {
        var store = new InMemoryLobbyStore();

        var result = store.CreateLobby(new CreateLobbyRequest(
            "Kaiju night",
            MaxPlayers: 4,
            IsPublic: true,
            HostDisplayName: "Host"));

        Assert.NotEqual(Guid.Empty, result.Lobby.LobbyId);
        Assert.Equal("Kaiju night", result.Lobby.Name);
        Assert.Equal(4, result.Lobby.MaxPlayers);
        Assert.True(result.Lobby.IsPublic);
        Assert.Equal(GameOptions.DefaultInitialHealth, result.Lobby.InitialHealth);
        Assert.Equal(GameOptions.DefaultTargetVictoryPoints, result.Lobby.TargetVictoryPoints);
        Assert.Equal(LobbyStatus.WaitingForPlayers, result.Lobby.Status);
        Assert.Null(result.Lobby.GameId);
        Assert.Single(result.Lobby.Seats);
        Assert.Equal(0, result.PlayerId);
        Assert.Equal(result.PlayerToken, result.Lobby.Seats[0].PlayerToken);
        Assert.Equal("Host", result.Lobby.Seats[0].DisplayName);
        Assert.True(result.Lobby.Seats[0].IsHost);
        Assert.True(result.Lobby.Seats[0].IsReady);
    }

    [Fact]
    public void CreateLobby_Should_ApplyCustomGameOptions()
    {
        var store = new InMemoryLobbyStore();

        var result = store.CreateLobby(new CreateLobbyRequest(
            "Custom game",
            MaxPlayers: 5,
            IsPublic: false,
            HostDisplayName: "Host",
            InitialHealth: 15,
            TargetVictoryPoints: 30));

        Assert.Equal(15, result.Lobby.InitialHealth);
        Assert.Equal(30, result.Lobby.TargetVictoryPoints);
    }

    [Fact]
    public void CreateLobby_Should_NormalizeBlankLobbyAndHostNames()
    {
        var store = new InMemoryLobbyStore();

        var result = store.CreateLobby(new CreateLobbyRequest(
            "  ",
            MaxPlayers: 2,
            IsPublic: false,
            HostDisplayName: "  "));

        Assert.Equal("King of Tokyo game", result.Lobby.Name);
        Assert.Equal("Monster", result.Lobby.Seats[0].DisplayName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void CreateLobby_Should_RejectInvalidMaxPlayers(int maxPlayers)
    {
        var store = new InMemoryLobbyStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateLobby(new CreateLobbyRequest(
            "Invalid",
            maxPlayers,
            IsPublic: true,
            HostDisplayName: "Host")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void CreateLobby_Should_RejectInvalidInitialHealth(int initialHealth)
    {
        var store = new InMemoryLobbyStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateLobby(new CreateLobbyRequest(
            "Invalid",
            MaxPlayers: 2,
            IsPublic: true,
            HostDisplayName: "Host",
            InitialHealth: initialHealth)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void CreateLobby_Should_RejectInvalidTargetVictoryPoints(int targetVictoryPoints)
    {
        var store = new InMemoryLobbyStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateLobby(new CreateLobbyRequest(
            "Invalid",
            MaxPlayers: 2,
            IsPublic: true,
            HostDisplayName: "Host",
            TargetVictoryPoints: targetVictoryPoints)));
    }

    [Fact]
    public void TryGetLobby_Should_ReturnCreatedLobbySnapshot()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));

        var found = store.TryGetLobby(created.Lobby.LobbyId, out var lobby);

        Assert.True(found);
        Assert.NotNull(lobby);
        Assert.Equal(created.Lobby.LobbyId, lobby!.LobbyId);
        Assert.Single(lobby.Seats);
    }

    [Fact]
    public void TryGetLobby_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TryGetLobby(Guid.NewGuid(), out var lobby);

        Assert.False(found);
        Assert.Null(lobby);
    }

    [Fact]
    public void TryJoinLobby_Should_AddNextSeatAndKeepLobbyWaitingUntilJoinedPlayerIsReady()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));

        var found = store.TryJoinLobby(
            created.Lobby.LobbyId,
            new JoinLobbyRequest("Guest"),
            out var joinResult,
            out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(joinResult);
        Assert.Equal(1, joinResult!.PlayerId);
        Assert.Equal("Guest", joinResult.Lobby.Seats[1].DisplayName);
        Assert.False(joinResult.Lobby.Seats[1].IsReady);
        Assert.Equal(LobbyStatus.WaitingForPlayers, joinResult.Lobby.Status);
    }

    [Fact]
    public void TryJoinLobby_Should_ReturnError_WhenLobbyIsFull()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out _, out _);

        var found = store.TryJoinLobby(
            created.Lobby.LobbyId,
            new JoinLobbyRequest("Extra"),
            out var result,
            out var error);

        Assert.True(found);
        Assert.Null(result);
        Assert.Equal("Lobby is full.", error);
    }

    [Fact]
    public void TryJoinLobby_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TryJoinLobby(Guid.NewGuid(), new JoinLobbyRequest("Guest"), out var result, out var error);

        Assert.False(found);
        Assert.Null(result);
        Assert.Null(error);
    }

    [Fact]
    public void TrySetReady_Should_MarkJoinedPlayerReadyAndMoveLobbyToReadyToStart()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);

        var found = store.TrySetReady(
            created.Lobby.LobbyId,
            new SetLobbyReadyRequest(joinResult!.PlayerToken, IsReady: true),
            out var lobby,
            out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(lobby);
        Assert.Equal(LobbyStatus.ReadyToStart, lobby!.Status);
        Assert.All(lobby.Seats, seat => Assert.True(seat.IsReady));
    }

    [Fact]
    public void TrySetReady_Should_ReturnError_WhenPlayerTokenIsUnknown()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TrySetReady(
            created.Lobby.LobbyId,
            new SetLobbyReadyRequest(Guid.NewGuid(), IsReady: true),
            out var lobby,
            out var error);

        Assert.True(found);
        Assert.Null(lobby);
        Assert.Equal("Player token was not found in this lobby.", error);
    }

    [Fact]
    public void TrySetReady_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TrySetReady(
            Guid.NewGuid(),
            new SetLobbyReadyRequest(Guid.NewGuid(), IsReady: true),
            out var lobby,
            out var error);

        Assert.False(found);
        Assert.Null(lobby);
        Assert.Null(error);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnGameRequest_WhenHostStartsReadyLobby()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest(
            "Game",
            MaxPlayers: 2,
            IsPublic: true,
            HostDisplayName: "Host",
            InitialHealth: 15,
            TargetVictoryPoints: 30));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);
        store.TrySetReady(created.Lobby.LobbyId, new SetLobbyReadyRequest(joinResult!.PlayerToken, IsReady: true), out _, out _);

        var found = store.TryPrepareStart(
            created.Lobby.LobbyId,
            new StartLobbyRequest(created.PlayerToken),
            out var preparation,
            out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(preparation);
        Assert.Equal(LobbyStatus.ReadyToStart, preparation!.Lobby.Status);
        Assert.Equal(new[] { "Host", "Guest" }, preparation.GameRequest.MonsterNames);
        Assert.Equal(15, preparation.GameRequest.InitialHealth);
        Assert.Equal(30, preparation.GameRequest.TargetVictoryPoints);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnError_WhenRequesterIsNotHost()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);
        store.TrySetReady(created.Lobby.LobbyId, new SetLobbyReadyRequest(joinResult!.PlayerToken, IsReady: true), out _, out _);

        var found = store.TryPrepareStart(
            created.Lobby.LobbyId,
            new StartLobbyRequest(joinResult.PlayerToken),
            out var preparation,
            out var error);

        Assert.True(found);
        Assert.Null(preparation);
        Assert.Equal("Only the host can start the lobby.", error);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnError_WhenLobbyHasOnlyHost()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TryPrepareStart(
            created.Lobby.LobbyId,
            new StartLobbyRequest(created.PlayerToken),
            out var preparation,
            out var error);

        Assert.True(found);
        Assert.Null(preparation);
        Assert.Equal("At least two players are required to start the lobby.", error);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnError_WhenSomePlayerIsNotReady()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out _, out _);

        var found = store.TryPrepareStart(
            created.Lobby.LobbyId,
            new StartLobbyRequest(created.PlayerToken),
            out var preparation,
            out var error);

        Assert.True(found);
        Assert.Null(preparation);
        Assert.Equal("All players must be ready before starting the lobby.", error);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TryPrepareStart(
            Guid.NewGuid(),
            new StartLobbyRequest(Guid.NewGuid()),
            out var preparation,
            out var error);

        Assert.False(found);
        Assert.Null(preparation);
        Assert.Null(error);
    }

    [Fact]
    public void TryAttachGame_Should_MarkLobbyStartedAndStoreGameId()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        var gameId = Guid.NewGuid();

        var found = store.TryAttachGame(created.Lobby.LobbyId, gameId, out var lobby, out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(lobby);
        Assert.Equal(LobbyStatus.Started, lobby!.Status);
        Assert.Equal(gameId, lobby.GameId);
    }

    [Fact]
    public void TryAttachGame_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TryAttachGame(Guid.NewGuid(), Guid.NewGuid(), out var lobby, out var error);

        Assert.False(found);
        Assert.Null(lobby);
        Assert.Null(error);
    }
}
