using System.Net.Http.Json;
using KingOfTokyo.Web.Contracts;

namespace KingOfTokyo.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LobbyJoinResultDto> CreateLobbyAsync(CreateLobbyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/lobbies", request, cancellationToken);
        return await ReadRequiredAsync<LobbyJoinResultDto>(response, cancellationToken);
    }

    public async Task<LobbyDto?> GetLobbyAsync(Guid lobbyId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<LobbyDto>($"api/lobbies/{lobbyId}", cancellationToken);
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

    public async Task<GameStateDto?> GetGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<GameStateDto>($"api/games/{gameId}", cancellationToken);
    }

    public async Task<GameEventCursorDto?> GetEventsAsync(Guid gameId, long after, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<GameEventCursorDto>($"api/games/{gameId}/events?after={after}", cancellationToken);
    }

    public Task<ApiCommandResultDto> InitializeGameAsync(Guid gameId, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "initialize", null, cancellationToken);

    public Task<ApiCommandResultDto> BeginTurnAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "begin-turn", request, cancellationToken);

    public Task<ApiCommandResultDto> RollDiceAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "roll-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> RerollDiceAsync(Guid gameId, RerollDiceRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "reroll-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> FinalizeDiceAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "finalize-dice", request, cancellationToken);

    public Task<ApiCommandResultDto> EndTurnAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "end-turn", request, cancellationToken);

    public Task<ApiCommandResultDto> AdvancePlayerAsync(Guid gameId, ActorRequest request, CancellationToken cancellationToken = default)
        => PostCommandAsync(gameId, "advance-player", request, cancellationToken);

    private async Task<ApiCommandResultDto> PostCommandAsync(Guid gameId, string commandName, object? request, CancellationToken cancellationToken)
    {
        var response = request is null
            ? await _httpClient.PostAsync($"api/games/{gameId}/commands/{commandName}", null, cancellationToken)
            : await _httpClient.PostAsJsonAsync($"api/games/{gameId}/commands/{commandName}", request, cancellationToken);

        return await ReadRequiredAsync<ApiCommandResultDto>(response, cancellationToken);
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
