using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Api.GameSessions;

public interface IGameSessionStore
{
    GameStateDto CreateGame(CreateGameRequest request);

    bool TryGetSnapshot(Guid gameId, out GameStateDto? snapshot);

    bool TryGetEvents(Guid gameId, long fromEventSequenceExclusive, out GameEventCursorDto? cursor);

    bool TryExecute(Guid gameId, Func<GameEngine, GameState, CommandResult> execute, out ApiCommandResultDto? result);
}
