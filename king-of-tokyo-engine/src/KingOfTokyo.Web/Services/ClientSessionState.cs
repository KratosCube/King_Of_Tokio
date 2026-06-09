using System.Text.Json;
using Microsoft.JSInterop;

namespace KingOfTokyo.Web.Services;

public sealed class ClientSessionState
{
    private const string StorageKey = "king-of-tokyo.client-session";
    private readonly IJSRuntime _jsRuntime;
    private bool _loaded;

    public ClientSessionState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Guid? LobbyId { get; set; }
    public Guid? GameId { get; set; }
    public int? PlayerId { get; set; }
    public Guid? PlayerToken { get; set; }
    public long LastEventSequence { get; set; }

    public bool HasLobbyIdentity => LobbyId.HasValue && PlayerId.HasValue && PlayerToken.HasValue;

    public async Task LoadAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        var json = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var persisted = JsonSerializer.Deserialize<PersistedClientSessionState>(json);
            if (persisted is null)
            {
                return;
            }

            LobbyId = persisted.LobbyId;
            GameId = persisted.GameId;
            PlayerId = persisted.PlayerId;
            PlayerToken = persisted.PlayerToken;
            LastEventSequence = persisted.LastEventSequence;
            await DebugLogAsync("session.load", new
            {
                storage = "sessionStorage",
                LobbyId,
                GameId,
                PlayerId,
                PlayerToken,
                LastEventSequence
            });
        }
        catch (JsonException)
        {
            await ClearAsync();
        }
    }

    public async Task SaveAsync()
    {
        var persisted = new PersistedClientSessionState(
            LobbyId,
            GameId,
            PlayerId,
            PlayerToken,
            LastEventSequence);
        var json = JsonSerializer.Serialize(persisted);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, json);
        await DebugLogAsync("session.save", new
        {
            storage = "sessionStorage",
            LobbyId,
            GameId,
            PlayerId,
            PlayerToken,
            LastEventSequence
        });
    }

    public async Task ClearAsync()
    {
        LobbyId = null;
        GameId = null;
        PlayerId = null;
        PlayerToken = null;
        LastEventSequence = 0;
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        await DebugLogAsync("session.clear", new { storage = "sessionStorage" });
    }

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

    public async Task DebugLogAsync(string scope, object payload)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("tokyoDebug.log", scope, payload);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed record PersistedClientSessionState(
        Guid? LobbyId,
        Guid? GameId,
        int? PlayerId,
        Guid? PlayerToken,
        long LastEventSequence);
}
