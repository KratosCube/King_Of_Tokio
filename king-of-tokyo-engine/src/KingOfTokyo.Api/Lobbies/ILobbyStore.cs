namespace KingOfTokyo.Api.Lobbies;

public interface ILobbyStore
{
    LobbyJoinResultDto CreateLobby(CreateLobbyRequest request);

    bool TryGetLobby(Guid lobbyId, out LobbyDto? lobby);

    bool TryJoinLobby(Guid lobbyId, JoinLobbyRequest request, out LobbyJoinResultDto? result, out string? error);

    bool TrySetReady(Guid lobbyId, SetLobbyReadyRequest request, out LobbyDto? lobby, out string? error);
}
