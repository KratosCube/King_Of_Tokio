using System.Collections.Concurrent;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Api.Lobbies;

public sealed class InMemoryLobbyStore : ILobbyStore
{
    private readonly ConcurrentDictionary<Guid, LobbyState> _lobbies = new();

    public LobbyJoinResultDto CreateLobby(CreateLobbyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateMaxPlayers(request.MaxPlayers);
        ValidateInitialHealth(request.InitialHealth);
        ValidateTargetVictoryPoints(request.TargetVictoryPoints);

        var lobby = new LobbyState(
            Guid.NewGuid(),
            NormalizeName(request.Name),
            request.MaxPlayers,
            request.IsPublic,
            request.InitialHealth,
            request.TargetVictoryPoints);
        var hostSeat = lobby.AddSeat(NormalizeDisplayName(request.HostDisplayName), isHost: true);
        hostSeat.IsReady = true;

        if (!_lobbies.TryAdd(lobby.LobbyId, lobby))
        {
            throw new InvalidOperationException("Could not create a unique lobby.");
        }

        return new LobbyJoinResultDto(ToDto(lobby), hostSeat.PlayerToken, hostSeat.PlayerId);
    }

    public bool TryGetLobby(Guid lobbyId, out LobbyDto? lobby)
    {
        lobby = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            lobby = ToDto(state);
            return true;
        }
    }

    public bool TryJoinLobby(Guid lobbyId, JoinLobbyRequest request, out LobbyJoinResultDto? result, out string? error)
    {
        ArgumentNullException.ThrowIfNull(request);
        result = null;
        error = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            if (state.Status is not LobbyStatus.WaitingForPlayers and not LobbyStatus.ReadyToStart)
            {
                error = "Lobby is not joinable.";
                return true;
            }

            if (state.Seats.Count >= state.MaxPlayers)
            {
                error = "Lobby is full.";
                return true;
            }

            var seat = state.AddSeat(NormalizeDisplayName(request.DisplayName), isHost: false);
            state.RecalculateStatus();
            result = new LobbyJoinResultDto(ToDto(state), seat.PlayerToken, seat.PlayerId);
            return true;
        }
    }

    public bool TrySetReady(Guid lobbyId, SetLobbyReadyRequest request, out LobbyDto? lobby, out string? error)
    {
        lobby = null;
        error = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            var seat = state.Seats.SingleOrDefault(s => s.PlayerToken == request.PlayerToken);
            if (seat is null)
            {
                error = "Player token was not found in this lobby.";
                return true;
            }

            if (state.Status is LobbyStatus.Started or LobbyStatus.Closed)
            {
                error = "Lobby readiness can no longer be changed.";
                return true;
            }

            seat.IsReady = request.IsReady;
            state.RecalculateStatus();
            lobby = ToDto(state);
            return true;
        }
    }

    private static void ValidateMaxPlayers(int maxPlayers)
    {
        if (maxPlayers is < 2 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), "Max players must be between 2 and 6.");
        }
    }

    private static void ValidateInitialHealth(int initialHealth)
    {
        if (initialHealth is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(initialHealth), "Initial health must be between 1 and 50.");
        }
    }

    private static void ValidateTargetVictoryPoints(int targetVictoryPoints)
    {
        if (targetVictoryPoints is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(targetVictoryPoints), "Target victory points must be between 1 and 100.");
        }
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "King of Tokyo game" : normalized;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        var normalized = displayName.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Monster" : normalized;
    }

    private static LobbyDto ToDto(LobbyState state)
    {
        return new LobbyDto(
            state.LobbyId,
            state.Name,
            state.MaxPlayers,
            state.IsPublic,
            state.InitialHealth,
            state.TargetVictoryPoints,
            state.Status,
            state.Seats
                .Select(seat => new LobbySeatDto(
                    seat.PlayerId,
                    seat.DisplayName,
                    seat.IsHost,
                    seat.IsReady,
                    seat.PlayerToken))
                .ToArray());
    }

    private sealed class LobbyState
    {
        public LobbyState(
            Guid lobbyId,
            string name,
            int maxPlayers,
            bool isPublic,
            int initialHealth,
            int targetVictoryPoints)
        {
            LobbyId = lobbyId;
            Name = name;
            MaxPlayers = maxPlayers;
            IsPublic = isPublic;
            InitialHealth = initialHealth;
            TargetVictoryPoints = targetVictoryPoints;
        }

        public Guid LobbyId { get; }
        public string Name { get; }
        public int MaxPlayers { get; }
        public bool IsPublic { get; }
        public int InitialHealth { get; }
        public int TargetVictoryPoints { get; }
        public LobbyStatus Status { get; private set; } = LobbyStatus.WaitingForPlayers;
        public List<LobbySeatState> Seats { get; } = new();
        public object SyncRoot { get; } = new();

        public LobbySeatState AddSeat(string displayName, bool isHost)
        {
            var seat = new LobbySeatState(
                Seats.Count,
                displayName,
                isHost,
                Guid.NewGuid());
            Seats.Add(seat);
            RecalculateStatus();
            return seat;
        }

        public void RecalculateStatus()
        {
            if (Status is LobbyStatus.Started or LobbyStatus.Closed)
            {
                return;
            }

            Status = Seats.Count >= 2 && Seats.All(seat => seat.IsReady)
                ? LobbyStatus.ReadyToStart
                : LobbyStatus.WaitingForPlayers;
        }
    }

    private sealed class LobbySeatState
    {
        public LobbySeatState(int playerId, string displayName, bool isHost, Guid playerToken)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            IsHost = isHost;
            PlayerToken = playerToken;
        }

        public int PlayerId { get; }
        public string DisplayName { get; }
        public bool IsHost { get; }
        public Guid PlayerToken { get; }
        public bool IsReady { get; set; }
    }
}
