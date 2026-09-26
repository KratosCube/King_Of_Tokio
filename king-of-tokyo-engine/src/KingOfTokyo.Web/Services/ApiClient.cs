using System.Net.Http.Json;
using KingOfTokyo.Web.Contracts;

namespace KingOfTokyo.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ClientSessionState _session;

    public ApiClient(HttpClient httpClient, ClientSessionState session)
    {
        _httpClient = httpClient;
        _session = session;
    }

    public async Task<LobbyJoinResultDto> CreateLobbyAsync(CreateLobbyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/lobbies", request, cancellationToken);
        return await ReadRequiredAsync<LobbyJoinResultDto>(response, cancellationToken);
    }

    public async Task<LobbyDto> GetLobbyAsync(Guid lobbyId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/lobbies/{lobbyId}", cancellationToken);
        return await ReadRequiredAsync<LobbyDto>(response, cancellationToken);
    }

    public async Task<LobbyJoinResultDto> JoinLobbyAsync(Guid lobbyId, JoinLobbyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/lobbies/{lobbyId}/join", request, cancellationToken);
        return await ReadRequiredAsync<LobbyJoinResultDto>(response, cancellationToken);
    }

    public async Task<LobbyDto> SetReadyAsync(Guid lobbyId, SetLobbyReadyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/lobbies/{lobbyId}/ready", request, cancellationToken);
        return await ReadRequiredAsync<LobbyDto>(response, cancellationToken);
    }

    public async Task<LobbyStartResultDto> StartLobbyAsync(Guid lobbyId, StartLobbyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/lobbies/{lobbyId}/start", request, cancellationToken);
        return await ReadRequiredAsync<LobbyStartResultDto>(response, cancellationToken);
    }

    public async Task<GameStateDto> GetGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        using var request = GameRequest(HttpMethod.Get, $"api/games/{gameId}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<GameStateDto>(response, cancellationToken);
    }

    public async Task<GameEventCursorDto> GetEventsAsync(Guid gameId, long after, CancellationToken cancellationToken = default)
    {
        using var request = GameRequest(HttpMethod.Get, $"api/games/{gameId}/events?after={after}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<GameEventCursorDto>(response, cancellationToken);
    }

    public Task<ApiCommandResultDto> InitializeGameAsync(Guid gameId, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "initialize", null, cancellationToken);

    public Task<ApiCommandResultDto> BeginTurnAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "begin-turn", request, cancellationToken);

    public Task<ApiCommandResultDto> RollDiceAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "roll-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> RerollDiceAsync(Guid gameId, RerollDiceRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "reroll-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> RerollBackgroundDwellerAsync(Guid gameId, RerollDiceRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "reroll-background-dweller-threes", request, cancellationToken);

    public Task<ApiCommandResultDto> FinalizeDiceAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "finalize-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> BuyFaceUpCardAsync(Guid gameId, BuyFaceUpCardRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "buy-face-up-card", request, cancellationToken);

    public Task<ApiCommandResultDto> RefreshMarketAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "refresh-market", request, cancellationToken);

    public Task<ApiCommandResultDto> ChooseLeaveTokyoAsync(Guid gameId, ChooseLeaveTokyoRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "choose-leave-tokyo", request, cancellationToken);

    public Task<ApiCommandResultDto> EndTurnAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "end-turn", request, cancellationToken);

    public Task<ApiCommandResultDto> AdvancePlayerAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "advance-player", request, cancellationToken);

    public Task<ApiCommandResultDto> BuyOpportunistAsync(Guid gameId, BatteryPurchaseRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "buy-opportunist-revealed-card", request, cancellationToken);

    public Task<ApiCommandResultDto> DeclineOpportunistAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "decline-opportunist-revealed-card", request, cancellationToken);

    public Task<ApiCommandResultDto> ActivateRapidHealingAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "activate-rapid-healing", request, cancellationToken);

    public Task<ApiCommandResultDto> ActivateHealingRayAsync(Guid gameId, HealingRayRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "activate-healing-ray", request, cancellationToken);

    public Task<ApiCommandResultDto> PeekTopDeckCardAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "peek-top-deck-card", request, cancellationToken);

    public Task<ApiCommandResultDto> BuyPeekedTopDeckCardAsync(Guid gameId, BatteryPurchaseRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "buy-peeked-top-deck-card", request, cancellationToken);

    public Task<ApiCommandResultDto> DeclinePeekedTopDeckCardAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "decline-peeked-top-deck-card", request, cancellationToken);

    private async Task<ApiCommandResultDto> PostCommandAsync(Guid gameId, string commandName, object? request, CancellationToken cancellationToken)
    {
        using var message = GameRequest(HttpMethod.Post, $"api/games/{gameId}/commands/{commandName}");
        if (request is not null)
        {
            message.Content = JsonContent.Create(request);
        }
        var response = await _httpClient.SendAsync(message, cancellationToken);

        return await ReadRequiredAsync<ApiCommandResultDto>(response, cancellationToken);
    }

    private HttpRequestMessage GameRequest(HttpMethod method, string path)
    {
        var message = new HttpRequestMessage(method, path);
        if (_session.PlayerToken is Guid token)
        {
            message.Headers.Add("X-Player-Token", token.ToString());
        }
        return message;
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"Request failed with status {(int)response.StatusCode}."
                : error);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Response body was empty or could not be deserialized.");
    }
}
