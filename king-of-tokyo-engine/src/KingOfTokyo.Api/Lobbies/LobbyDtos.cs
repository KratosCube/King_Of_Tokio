using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;

namespace KingOfTokyo.Api.Lobbies;

public sealed record CreateLobbyRequest(
    string Name,
    int MaxPlayers,
    bool IsPublic,
    string HostDisplayName,
    int InitialHealth = GameOptions.DefaultInitialHealth,
    int TargetVictoryPoints = GameOptions.DefaultTargetVictoryPoints,
    string? HostMonsterId = null,
    string? HostMonsterName = null,
    string? HostAvatarId = null);

public sealed record JoinLobbyRequest(
    string DisplayName,
    string? MonsterId = null,
    string? MonsterName = null,
    string? AvatarId = null);

public sealed record SetLobbyReadyRequest(Guid PlayerToken, bool IsReady);

public sealed record StartLobbyRequest(Guid PlayerToken);

public sealed record LobbyDto(
    Guid LobbyId,
    string Name,
    int MaxPlayers,
    bool IsPublic,
    int InitialHealth,
    int TargetVictoryPoints,
    LobbyStatus Status,
    Guid? GameId,
    IReadOnlyList<LobbySeatDto> Seats);

public sealed record LobbySeatDto(
    int PlayerId,
    string DisplayName,
    bool IsHost,
    bool IsReady,
    string MonsterId,
    string MonsterName,
    string AvatarId);

public sealed record LobbyJoinResultDto(
    LobbyDto Lobby,
    Guid PlayerToken,
    int PlayerId);

public sealed record LobbyStartPreparationDto(
    LobbyDto Lobby,
    CreateGameRequest GameRequest,
    IReadOnlyDictionary<int, Guid> PlayerTokens);

public sealed record LobbyStartResultDto(
    LobbyDto Lobby,
    GameStateDto Game);

public enum LobbyStatus
{
    WaitingForPlayers = 0,
    ReadyToStart = 1,
    Started = 2,
    Closed = 3,
    Starting = 4
}
