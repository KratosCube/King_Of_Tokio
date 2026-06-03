using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public static class GameStateValidatorWingsExtensions
{
    public static void EnsureCanActivateWings(
        this GameStateValidator validator,
        GameState gameState,
        ActivateWingsCommand command)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Wings when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Wings without an active turn.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("ActivateWingsCommand requires an actor player id.");
        }

        var player = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!player.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot activate Wings.");
        }

        var keepCardRulesService = new KeepCardRulesService();
        if (!keepCardRulesService.CanUseWings(player))
        {
            if (player.Energy < KeepCardRulesService.WingsCost)
            {
                throw new InvalidOperationException("Player does not have enough energy to activate Wings.");
            }

            throw new InvalidOperationException("Player cannot use Wings right now.");
        }

        if (gameState.PendingDecision is not null &&
            gameState.PendingDecision.DecisionType != DecisionType.LeaveTokyo)
        {
            throw new InvalidOperationException("Cannot activate Wings while another decision is pending.");
        }

        if (gameState.PendingDecision is not null &&
            gameState.PendingDecision.PlayerId != player.PlayerId)
        {
            throw new InvalidOperationException("Only the pending Tokyo defender can activate Wings right now.");
        }

        if (GetNetDamageTakenThisTurn(gameState, player.PlayerId) <= 0)
        {
            throw new InvalidOperationException("Player has not taken damage during this turn.");
        }

        if (player.Health >= player.MaxHealth)
        {
            throw new InvalidOperationException("Player has no damage left to cancel.");
        }
    }

    internal static int GetNetDamageTakenThisTurn(GameState gameState, int playerId)
    {
        var currentTurnPlayerId = gameState.CurrentTurn?.CurrentPlayerId;
        var eventsThisTurn = currentTurnPlayerId.HasValue
            ? gameState.EventLog
                .Reverse()
                .TakeWhile(gameEvent => gameEvent is not TurnStartedEvent started || started.PlayerId != currentTurnPlayerId.Value)
                .Reverse()
            : gameState.EventLog;

        var damageTaken = eventsThisTurn
            .OfType<DamageDealtEvent>()
            .Where(gameEvent => gameEvent.TargetPlayerId == playerId)
            .Sum(gameEvent => gameEvent.Amount);

        var damageCanceled = eventsThisTurn
            .OfType<DamageCanceledEvent>()
            .Where(gameEvent => gameEvent.PlayerId == playerId)
            .Sum(gameEvent => gameEvent.Amount);

        return Math.Max(0, damageTaken - damageCanceled);
    }
}
