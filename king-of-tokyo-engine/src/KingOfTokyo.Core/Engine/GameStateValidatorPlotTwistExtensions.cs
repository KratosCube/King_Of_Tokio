using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Engine;

public static class GameStateValidatorPlotTwistExtensions
{
    public static void EnsureCanActivatePlotTwist(
        this GameStateValidator validator,
        GameState gameState,
        ActivatePlotTwistCommand command)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Plot Twist when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Plot Twist without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            if (gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll)
            {
                throw new InvalidOperationException("Cannot activate Plot Twist while another decision is pending.");
            }

            if (gameState.PendingDecision.PlayerId != gameState.CurrentTurn.CurrentPlayerId)
            {
                throw new InvalidOperationException("Plot Twist decision belongs to a different player.");
            }
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Plot Twist can only be used during the rolling phase before dice are finalized.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Plot Twist can only be used after at least one roll.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (command.DieIndex < 0 || command.DieIndex >= gameState.CurrentTurn.DicePool.Dice.Count)
        {
            throw new InvalidOperationException("Selected die index is invalid.");
        }

        if (!currentPlayer.HasKeepCard(KnownCardIds.PlotTwist))
        {
            throw new InvalidOperationException("Player cannot use Plot Twist right now.");
        }
    }
}