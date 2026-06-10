using System.Collections.Concurrent;
using KingOfTokyo.Api.Contracts;

namespace KingOfTokyo.Api.Lobbies;

public sealed class InMemoryLobbyStore : ILobbyStore
{
    private const string DefaultMonsterId = "gigasaur";
    private const string DefaultMonsterName = "Gigasaur";
    private const string DefaultAvatarId = "avatar-roar";

    private readonly ConcurrentDictionary<Guid, LobbyState> _lobbies = new();
    private readonly Random _random;

    public InMemoryLobbyStore(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

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
        var hostSeat = lobby.AddSeat(
            NormalizeDisplayName(request.HostDisplayName),
            isHost: true,
            NormalizeId(request.HostMonsterId, DefaultMonsterId),
            NormalizeNameOrDefault(request.HostMonsterName, DefaultMonsterName),
            NormalizeId(request.HostAvatarId, DefaultAvatarId));
        hostSeat.IsReady = true;

        if (!_lobbies.TryAdd(lobby.LobbyId, lobby))
        {
            throw new InvalidOperationException("Could not create a unique lobby.");
        }

        return new LobbyJoinResultDto(ToDto(lobby), hostSeat.PlayerToken, hostSeat.PlayerId);
    }

    public IReadOnlyList<LobbyDto> ListLobbies(bool publicOnly = true)
    {
        var lobbies = new List<LobbyDto>();

        foreach (var state in _lobbies.Values)
        {
            lock (state.SyncRoot)
            {
                if (publicOnly && !state.IsPublic)
                {
                    continue;
                }

                lobbies.Add(ToDto(state));
            }
        }

        return lobbies
            .OrderBy(lobby => lobby.Status is LobbyStatus.WaitingForPlayers or LobbyStatus.ReadyToStart ? 0 : 1)
            .ThenBy(lobby => lobby.Name)
            .ToArray();
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

            var seat = state.AddSeat(
                NormalizeDisplayName(request.DisplayName),
                isHost: false,
                NormalizeId(request.MonsterId, DefaultMonsterId),
                NormalizeNameOrDefault(request.MonsterName, DefaultMonsterName),
                NormalizeId(request.AvatarId, DefaultAvatarId));
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

    public bool TryLeaveLobby(Guid lobbyId, LeaveLobbyRequest request, out LobbyLeaveResultDto? result, out string? error)
    {
        result = null;
        error = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            if (state.Status is LobbyStatus.Started)
            {
                error = "Cannot leave a lobby after the game has started.";
                return true;
            }

            var seat = state.Seats.SingleOrDefault(s => s.PlayerToken == request.PlayerToken);
            if (seat is null)
            {
                error = "Player token was not found in this lobby.";
                return true;
            }

            state.RemoveSeat(seat);
            if (state.Seats.Count == 0)
            {
                _lobbies.TryRemove(lobbyId, out _);
                result = new LobbyLeaveResultDto(Deleted: true, Lobby: null);
                return true;
            }

            state.EnsureHost();
            state.RecalculateStatus();
            result = new LobbyLeaveResultDto(Deleted: false, ToDto(state));
            return true;
        }
    }

    public bool TryPrepareStart(Guid lobbyId, StartLobbyRequest request, out LobbyStartPreparationDto? result, out string? error)
    {
        result = null;
        error = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            var requester = state.Seats.SingleOrDefault(s => s.PlayerToken == request.PlayerToken);
            if (requester is null)
            {
                error = "Player token was not found in this lobby.";
                return true;
            }

            if (!requester.IsHost)
            {
                error = "Only the host can start the lobby.";
                return true;
            }

            if (state.Status is LobbyStatus.Started)
            {
                error = "Lobby has already been started.";
                return true;
            }

            if (state.Status is LobbyStatus.Closed)
            {
                error = "Lobby is closed.";
                return true;
            }

            if (state.Seats.Count < 2)
            {
                error = "At least two players are required to start the lobby.";
                return true;
            }

            if (state.Seats.Any(seat => !seat.IsReady))
            {
                error = "All players must be ready before starting the lobby.";
                return true;
            }

            var gameSeats = state.AssignRandomGamePlayerOrder(_random);
            var gameRequest = new CreateGameRequest(
                gameSeats.Select(seat => seat.MonsterName).ToArray(),
                InitialHealth: state.InitialHealth,
                TargetVictoryPoints: state.TargetVictoryPoints);
            result = new LobbyStartPreparationDto(ToDto(state), gameRequest);
            return true;
        }
    }

    public bool TryAttachGame(Guid lobbyId, Guid gameId, out LobbyDto? lobby, out string? error)
    {
        lobby = null;
        error = null;

        if (!_lobbies.TryGetValue(lobbyId, out var state))
        {
            return false;
        }

        lock (state.SyncRoot)
        {
            if (state.Status is LobbyStatus.Closed)
            {
                error = "Lobby is closed.";
                return true;
            }

            if (state.Status is LobbyStatus.Started && state.GameId != gameId)
            {
                error = "Lobby has already been started.";
                return true;
            }

            state.AttachGame(gameId);
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

    private static string NormalizeNameOrDefault(string? value, string fallback)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string NormalizeId(string? value, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
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
            state.GameId,
            state.Seats
                .OrderBy(seat => seat.PlayerId)
                .Select(seat => new LobbySeatDto(
                    seat.PlayerId,
                    seat.DisplayName,
                    seat.IsHost,
                    seat.IsReady,
                    seat.PlayerToken,
                    seat.MonsterId,
                    seat.MonsterName,
                    seat.AvatarId))
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
        public Guid? GameId { get; private set; }
        public List<LobbySeatState> Seats { get; } = new();
        public object SyncRoot { get; } = new();

        public LobbySeatState AddSeat(string displayName, bool isHost, string monsterId, string monsterName, string avatarId)
        {
            var seat = new LobbySeatState(
                Seats.Count == 0 ? 0 : Seats.Max(existingSeat => existingSeat.PlayerId) + 1,
                displayName,
                isHost,
                Guid.NewGuid(),
                monsterId,
                monsterName,
                avatarId);
            Seats.Add(seat);
            RecalculateStatus();
            return seat;
        }

        public void RemoveSeat(LobbySeatState seat)
        {
            Seats.Remove(seat);
        }

        public void EnsureHost()
        {
            if (Seats.Count == 0 || Seats.Any(seat => seat.IsHost))
            {
                return;
            }

            Seats[0].IsHost = true;
        }

        public IReadOnlyList<LobbySeatState> AssignRandomGamePlayerOrder(Random random)
        {
            ArgumentNullException.ThrowIfNull(random);

            var shuffled = Seats.ToArray();
            lock (random)
            {
                for (var i = shuffled.Length - 1; i > 0; i--)
                {
                    var swapIndex = random.Next(i + 1);
                    (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
                }
            }

            Seats.Clear();
            Seats.AddRange(shuffled);

            for (var playerId = 0; playerId < Seats.Count; playerId++)
            {
                Seats[playerId].SetPlayerId(playerId);
            }

            return Seats.ToArray();
        }

        public void AttachGame(Guid gameId)
        {
            GameId = gameId;
            Status = LobbyStatus.Started;
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
        public LobbySeatState(
            int playerId,
            string displayName,
            bool isHost,
            Guid playerToken,
            string monsterId,
            string monsterName,
            string avatarId)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            IsHost = isHost;
            PlayerToken = playerToken;
            MonsterId = monsterId;
            MonsterName = monsterName;
            AvatarId = avatarId;
        }

        public int PlayerId { get; private set; }
        public string DisplayName { get; }
        public bool IsHost { get; set; }
        public Guid PlayerToken { get; }
        public string MonsterId { get; }
        public string MonsterName { get; }
        public string AvatarId { get; }
        public bool IsReady { get; set; }

        public void SetPlayerId(int playerId)
        {
            if (playerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId));
            }

            PlayerId = playerId;
        }
    }
}