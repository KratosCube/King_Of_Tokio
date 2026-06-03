using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public static class GameEngineMimickedRapidHealingExtensions
{
    public static CommandResult Execute(this GameEngine engine, GameState gameState, ActivateMimickedRapidHealingCommand command)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            EnsureCanActivateMimickedRapidHealing(gameState, command);

            var rapidHealingService = new RapidHealingService();
            var stepResult = rapidHealingService.Activate(gameState);

            return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(gameState, ex.Message);
        }
    }

    private static void EnsureCanActivateMimickedRapidHealing(GameState gameState, ActivateMimickedRapidHealingCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Rapid Healing when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Rapid Healing without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot activate Rapid Healing while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase is TurnPhase.Finished or TurnPhase.TurnEnd or TurnPhase.NotStarted)
        {
            throw new InvalidOperationException("Rapid Healing cannot be used in the current phase.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (!HasMimicTarget(currentPlayer, KnownCardIds.RapidHealing))
        {
            throw new InvalidOperationException("Player cannot use mimicked Rapid Healing right now.");
        }

        if (currentPlayer.Energy < RapidHealingService.ActivationCost || currentPlayer.Health >= currentPlayer.MaxHealth)
        {
            throw new InvalidOperationException("Player cannot use mimicked Rapid Healing right now.");
        }
    }

    private static bool HasMimicTarget(PlayerState player, string cardId)
    {
        return player.KeepCards.Any(card =>
            card.CardId == KnownCardIds.Mimic &&
            card.MimicTarget?.CardId == cardId);
    }
}
