using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.Lobbies;
using Xunit;

namespace KingOfTokyo.Api.Tests.Lobbies;

public sealed class InMemoryLobbyStoreTests
{
    [Fact]
    public void CreateLobby_Should_CreateHostSeatReadyByDefault()
    {
        var store = new InMemoryLobbyStore();

        var result = store.CreateLobby(new CreateLobbyRequest("Game", 4, true, "Host"));

        Assert.NotEqual(Guid.Empty, result.Lobby.LobbyId);
        Assert.Equal("Game", result.Lobby.Name);
        Assert.Equal(4, result.Lobby.MaxPlayers);
        Assert.True(result.Lobby.IsPublic);
        Assert.Equal(LobbyStatus.WaitingForPlayers, result.Lobby.Status);
        Assert.Single(result.Lobby.Seats);
        Assert.Equal(0, result.PlayerId);
        Assert.Equal(result.PlayerToken, result.Lobby.Seats[0].PlayerToken);
        Assert.True(result.Lobby.Seats[0].IsHost);
        Assert.True(result.Lobby.Seats[0].IsReady);
    }

    [Fact]
    public void CreateLobby_Should_ClampBlankLobbyAndHostNames()
    {
        var store = new InMemoryLobbyStore();

        var result = store.CreateLobby(new CreateLobbyRequest("   ", 2, true, "   "));

        Assert.Equal("King of Tokyo game", result.Lobby.Name);
        Assert.Equal("Monster", result.Lobby.Seats[0].DisplayName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void CreateLobby_Should_RejectInvalidMaxPlayers(int maxPlayers)
    {
        var store = new InMemoryLobbyStore();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateLobby(new CreateLobbyRequest("Game", maxPlayers, true, "Host")));
    }

    [Fact]
    public void ListLobbies_Should_ReturnOnlyPublicLobbiesByDefault()
    {
        var store = new InMemoryLobbyStore();
        store.CreateLobby(new CreateLobbyRequest("Public", 2, true, "Host"));
        store.CreateLobby(new CreateLobbyRequest("Private", 2, false, "Host"));

        var result = store.ListLobbies();

        Assert.Single(result);
        Assert.Equal("Public", result[0].Name);
    }

    [Fact]
    public void ListLobbies_Should_ReturnPrivateLobbies_WhenPublicOnlyFalse()
    {
        var store = new InMemoryLobbyStore();
        store.CreateLobby(new CreateLobbyRequest("Public", 2, true, "Host"));
        store.CreateLobby(new CreateLobbyRequest("Private", 2, false, "Host"));

        var result = store.ListLobbies(publicOnly: false);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, lobby => lobby.Name == "Public");
        Assert.Contains(result, lobby => lobby.Name == "Private");
    }

    [Fact]
    public void TryGetLobby_Should_ReturnExistingLobby()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TryGetLobby(created.Lobby.LobbyId, out var lobby);

        Assert.True(found);
        Assert.NotNull(lobby);
        Assert.Equal(created.Lobby.LobbyId, lobby!.LobbyId);
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
    public void TryJoinLobby_Should_AddSeat()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));

        var found = store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var result, out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Lobby.Seats.Count);
        Assert.Equal(1, result.PlayerId);
        Assert.Equal(result.PlayerToken, result.Lobby.Seats[1].PlayerToken);
    }

    [Fact]
    public void TryJoinLobby_Should_SetReadyStatusToReadyToStart_WhenAllSeatsReady()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);

        store.TrySetReady(created.Lobby.LobbyId, new SetLobbyReadyRequest(joinResult!.PlayerToken, IsReady: true), out var lobby, out var error);

        Assert.Null(error);
        Assert.NotNull(lobby);
        Assert.Equal(LobbyStatus.ReadyToStart, lobby!.Status);
    }

    [Fact]
    public void TryJoinLobby_Should_ReturnError_WhenLobbyIsFull()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out _, out _);

        var found = store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Extra"), out var result, out var error);

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
    public void TrySetReady_Should_UpdateSeatReadyStatus()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);

        var found = store.TrySetReady(created.Lobby.LobbyId, new SetLobbyReadyRequest(joinResult!.PlayerToken, IsReady: true), out var lobby, out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(lobby);
        Assert.True(lobby!.Seats.Single(seat => seat.PlayerToken == joinResult.PlayerToken).IsReady);
    }

    [Fact]
    public void TrySetReady_Should_ReturnError_WhenTokenIsUnknown()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TrySetReady(created.Lobby.LobbyId, new SetLobbyReadyRequest(Guid.NewGuid(), IsReady: true), out var lobby, out var error);

        Assert.True(found);
        Assert.Null(lobby);
        Assert.Equal("Player token was not found in this lobby.", error);
    }

    [Fact]
    public void TryLeaveLobby_Should_RemoveSeat()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);

        var found = store.TryLeaveLobby(created.Lobby.LobbyId, new LeaveLobbyRequest(joinResult!.PlayerToken), out var result, out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.False(result!.Deleted);
        Assert.Single(result.Lobby!.Seats);
        Assert.DoesNotContain(result.Lobby.Seats, seat => seat.PlayerToken == joinResult.PlayerToken);
    }

    [Fact]
    public void TryLeaveLobby_Should_DeleteLobby_WhenLastSeatLeaves()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TryLeaveLobby(created.Lobby.LobbyId, new LeaveLobbyRequest(created.PlayerToken), out var result, out var error);

        Assert.True(found);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.True(result!.Deleted);
        Assert.Null(result.Lobby);
        Assert.False(store.TryGetLobby(created.Lobby.LobbyId, out _));
    }

    [Fact]
    public void TryLeaveLobby_Should_ReassignHost_WhenHostLeaves()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 3, true, "Host"));
        store.TryJoinLobby(created.Lobby.LobbyId, new JoinLobbyRequest("Guest"), out var joinResult, out _);

        store.TryLeaveLobby(created.Lobby.LobbyId, new LeaveLobbyRequest(created.PlayerToken), out var result, out var error);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.True(result!.Lobby!.Seats.Single().IsHost);
        Assert.Equal(joinResult!.PlayerToken, result.Lobby.Seats.Single().PlayerToken);
    }

    [Fact]
    public void TryLeaveLobby_Should_ReturnError_WhenTokenIsUnknown()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest("Game", 2, true, "Host"));

        var found = store.TryLeaveLobby(created.Lobby.LobbyId, new LeaveLobbyRequest(Guid.NewGuid()), out var result, out var error);

        Assert.True(found);
        Assert.Null(result);
        Assert.Equal("Player token was not found in this lobby.", error);
    }

    [Fact]
    public void TryLeaveLobby_Should_ReturnFalse_WhenLobbyDoesNotExist()
    {
        var store = new InMemoryLobbyStore();

        var found = store.TryLeaveLobby(Guid.NewGuid(), new LeaveLobbyRequest(Guid.NewGuid()), out var result, out var error);

        Assert.False(found);
        Assert.Null(result);
        Assert.Null(error);
    }

    [Fact]
    public void TryPrepareStart_Should_ReturnGameRequestWithMonsterNames_WhenHostStartsReadyLobby()
    {
        var store = new InMemoryLobbyStore();
        var created = store.CreateLobby(new CreateLobbyRequest(
            "Game",
            MaxPlayers: 2,
            IsPublic: true,
            HostDisplayName: "Host",
            InitialHealth: 15,
            TargetVictoryPoints: 30,
            HostMonsterId: "gigasaur",
            HostMonsterName: "Gigasaur",
            HostAvatarId: "avatar-roar"));
        store.TryJoinLobby(
            created.Lobby.LobbyId,
            new JoinLobbyRequest("Guest", "cyber-kitty", "Cyber Kitty", "avatar-neon"),
            out var joinResult,
            out _);
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
        Assert.Equal(new[] { "Gigasaur", "Cyber Kitty" }.OrderBy(name => name), preparation.GameRequest.MonsterNames.OrderBy(name => name));
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
}
