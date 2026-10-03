using System.Collections.Concurrent;
using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Api.GameSessions;

public sealed class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public GameStateDto CreateGame(CreateGameRequest request, IReadOnlyDictionary<int, Guid>? playerTokens = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var names = request.MonsterNames
            .Select(name => string.IsNullOrWhiteSpace(name) ? null : name.Trim())
            .ToArray();

        var playerCount = names.Length;
        var initialHealth = request.InitialHealth ?? GameOptions.DefaultInitialHealth;
        var targetVictoryPoints = request.TargetVictoryPoints ?? GameOptions.DefaultTargetVictoryPoints;
        var players = names
            .Select((name, index) => new PlayerState(index, name ?? $"Monster {index + 1}", initialHealth))
            .ToArray();
        var gameOptions = new GameOptions(
            playerCount,
            initialHealth: initialHealth,
            targetVictoryPoints: targetVictoryPoints);
        var gameState = new GameState(players, gameOptions);
        if (playerTokens is not null)
        {
            var tied = players.Select(player => player.PlayerId).ToArray();
            while (tied.Length > 1)
            {
                var rolls = tied.Select(id => (Id: id, Attacks: Enumerable.Range(0, 6).Count(_ => Random.Shared.Next(6) == 0))).ToArray();
                var highest = rolls.Max(roll => roll.Attacks);
                tied = rolls.Where(roll => roll.Attacks == highest).Select(roll => roll.Id).ToArray();
            }
            gameState.SelectStartingPlayer(tied[0]);
        }
        if (playerTokens is not null &&
            (playerTokens.Count != playerCount ||
             Enumerable.Range(0, playerCount).Any(index => !playerTokens.TryGetValue(index, out var token) || token == Guid.Empty) ||
             playerTokens.Values.Distinct().Count() != playerCount))
        {
            throw new ArgumentException("Each player must have a distinct nonempty token.", nameof(playerTokens));
        }

        var session = new GameSession(gameState, new GameEngine(marketSetupService: new MarketSetupService(shuffleDeck: true)), playerTokens);

        if (!_sessions.TryAdd(gameState.GameId, session))
        {
            throw new InvalidOperationException("Could not create a unique game session.");
        }

        return gameState.ToDto();
    }

    public bool TryAuthorize(Guid gameId, Guid playerToken, out int playerId)
    {
        playerId = -1;
        return playerToken != Guid.Empty &&
               _sessions.TryGetValue(gameId, out var session) &&
               session.PlayerIdsByToken.TryGetValue(playerToken, out playerId);
    }

    public bool TryGetSnapshot(Guid gameId, out GameStateDto? snapshot)
    {
        snapshot = null;

        if (!_sessions.TryGetValue(gameId, out var session))
        {
            return false;
        }

        lock (session.SyncRoot)
        {
            snapshot = session.GameState.ToDto();
            return true;
        }
    }

    public bool TryGetEvents(Guid gameId, long fromEventSequenceExclusive, out GameEventCursorDto? cursor)
    {
        cursor = null;

        if (!_sessions.TryGetValue(gameId, out var session))
        {
            return false;
        }

        lock (session.SyncRoot)
        {
            cursor = GameEventCursorMapper.MapEventsSince(session.GameState, fromEventSequenceExclusive);
            return true;
        }
    }

    public bool TryExecute(Guid gameId, Func<GameEngine, GameState, CommandResult> execute, out ApiCommandResultDto? result)
    {
        ArgumentNullException.ThrowIfNull(execute);
        result = null;

        if (!_sessions.TryGetValue(gameId, out var session))
        {
            return false;
        }

        lock (session.SyncRoot)
        {
            var commandResult = execute(session.Engine, session.GameState);
            result = ApiCommandResultDto.From(commandResult);
            return true;
        }
    }

    private sealed class GameSession
    {
        public GameSession(GameState gameState, GameEngine engine, IReadOnlyDictionary<int, Guid>? playerTokens)
        {
            GameState = gameState;
            Engine = engine;
            PlayerIdsByToken = playerTokens?.ToDictionary(pair => pair.Value, pair => pair.Key)
                ?? new Dictionary<Guid, int>();
        }

        public GameState GameState { get; }
        public GameEngine Engine { get; }
        public IReadOnlyDictionary<Guid, int> PlayerIdsByToken { get; }
        public object SyncRoot { get; } = new();
    }
}
