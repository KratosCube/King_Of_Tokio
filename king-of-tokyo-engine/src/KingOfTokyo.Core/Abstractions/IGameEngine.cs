using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Core.Abstractions;

public interface IGameEngine
{
    CommandResult Execute(GameState gameState, IGameCommand command);
}