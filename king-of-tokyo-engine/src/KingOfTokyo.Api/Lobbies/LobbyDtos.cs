namespace KingOfTokyo.Api.Lobbies;

public sealed record CreateLobbyRequest(
    string Name,
    int MaxPlayers,
    bool IsPublic,
    string HostDisplayName);

public sealed record JoinLobbyRequest(string DisplayName);

public sealed record SetLobbyReadyRequest(Guid PlayerToken, bool IsReady);

public sealed record LobbyDto(
    Guid LobbyId,
    string Name,
    int MaxPlayers,
    bool IsPublic,
    LobbyStatus Status,
    IReadOnlyList<LobbySeatDto> Seats);

public sealed record LobbySeatDto(
    int PlayerId,
    string DisplayName,
    bool IsHost,
    bool IsReady,
    Guid PlayerToken);

public sealed record LobbyJoinResultDto(
    LobbyDto Lobby,
    Guid PlayerToken,
    int PlayerId);

public enum LobbyStatus
{
    WaitingForPlayers = 0,
    ReadyToStart = 1,
    Started = 2,
    Closed = 3
}
