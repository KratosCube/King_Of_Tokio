using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Engine;

public static class GameEngineHealingRayExtensions
{
    public static CommandResult Execute(this GameEngine engine, GameState gameState, ActivateHealingRayCommand command)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        return engine.Execute(gameState, (KingOfTokyo.Core.Abstractions.IGameCommand)command);
    }
}
