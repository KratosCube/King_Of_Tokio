using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public static class GameStateValidatorSmokeCloudExtensions
{
    public static void EnsureCanActivateSmokeCloud(
        this GameStateValidator validator,
        GameState gameState,
        ActivateSmokeCloudCommand command)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Smoke Cloud when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Smoke Cloud without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Smoke Cloud can only be used during the rolling phase before dice are finalized.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Smoke Cloud can only be used after at least one roll.");
        }

        if (gameState.PendingDecision is not null &&
            gameState.PendingDecision.DecisionType != KingOfTokyo.Core.Decisions.DecisionType.SelectDiceToReroll)
        {
            throw new InvalidOperationException("Cannot activate Smoke Cloud while another decision is pending.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();
        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        var keepCardRulesService = new KeepCardRulesService();
        if (!keepCardRulesService.CanUseSmokeCloud(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Smoke Cloud right now.");
        }
    }
}
