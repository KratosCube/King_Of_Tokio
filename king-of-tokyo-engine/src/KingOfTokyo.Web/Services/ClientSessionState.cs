namespace KingOfTokyo.Web.Services;

public sealed class ClientSessionState
{
    public Guid? LobbyId { get; set; }
    public Guid? GameId { get; set; }
    public int? PlayerId { get; set; }
    public Guid? PlayerToken { get; set; }
    public long LastEventSequence { get; set; }

    public bool HasLobbyIdentity => LobbyId.HasValue && PlayerId.HasValue && PlayerToken.HasValue;

    public void RememberLobby(Guid lobbyId, int playerId, Guid playerToken)
    {
        LobbyId = lobbyId;
        PlayerId = playerId;
        PlayerToken = playerToken;
    }

    public void RememberGame(Guid gameId)
    {
        GameId = gameId;
        LastEventSequence = 0;
    }
}
