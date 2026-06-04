using System.Collections.Concurrent;
using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Api.GameSessions;

public sealed class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public GameStateDto CreateGame(IReadOnlyList<string> monsterNames)
    {
        ArgumentNullException.ThrowIfNull(monsterNames);

        var names = monsterNames
            .Select(name => string.IsNullOrWhiteSpace(name) ? null : name.Trim())
            .ToArray();

        var playerCount = names.Length;
        var players = names
            .Select((name, index) => new PlayerState(index, name ?? $"Monster {index + 1}"))
            .ToArray();
        var gameState = new GameState(players, new GameOptions(playerCount));
        var session = new GameSession(gameState, new GameEngine());

        if (!_sessions.TryAdd(gameState.GameId, session))
        {
            throw new InvalidOperationException("Could not create a unique game session.");
        }

        return gameState.ToDto();
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
        public GameSession(GameState gameState, GameEngine engine)
        {
            GameState = gameState;
            Engine = engine;
        }

        public GameState GameState { get; }
        public GameEngine Engine { get; }
        public object SyncRoot { get; } = new();
    }
}
