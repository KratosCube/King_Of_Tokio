using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public static class GameEngineHealingRayExtensions
{
    public static CommandResult Execute(this GameEngine engine, GameState gameState, ActivateHealingRayCommand command)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var (healer, target, currentTurn) = EnsureCanActivateHealingRay(gameState, command);
            var healingRayService = new HealingRayService();
            var result = healingRayService.HealOtherPlayer(healer, target, command.HealingAmount);
            currentTurn.SpendHealingRayHearts(command.HealingAmount);

            var events = new List<GameEventBase>();
            if (result.HealedAmount > 0)
            {
                events.Add(new PlayerHealedEvent(
                    target.PlayerId,
                    result.HealedAmount,
                    "Keep card: Healing Ray."));
            }

            return CommandResult.Successful(gameState, events);
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(gameState, ex.Message);
        }
    }

    private static (PlayerState Healer, PlayerState Target, TurnState CurrentTurn) EnsureCanActivateHealingRay(
        GameState gameState,
        ActivateHealingRayCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Healing Ray when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Healing Ray without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase &&
            gameState.CurrentTurn.Phase != TurnPhase.DiceResolved)
        {
            throw new InvalidOperationException("Healing Ray can only be used after dice are resolved and before the turn ends.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot activate Healing Ray while a decision is pending.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("ActivateHealingRayCommand requires an actor player id.");
        }

        if (gameState.CurrentTurn.CurrentPlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Only the current player can activate Healing Ray.");
        }

        var healer = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!healer.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot activate Healing Ray.");
        }

        var keepCardEffectLookupService = new KeepCardEffectLookupService();
        if (!keepCardEffectLookupService.HasEffect(healer, KnownCardIds.HealingRay))
        {
            throw new InvalidOperationException("Player does not have Healing Ray.");
        }

        var target = gameState.GetPlayerById(command.TargetPlayerId);
        if (!target.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot be healed by Healing Ray.");
        }

        if (healer.PlayerId == target.PlayerId)
        {
            throw new InvalidOperationException("Healing Ray can only heal other players.");
        }

        var heartCount = gameState.CurrentTurn.DicePool.Dice.Count(die => die.CurrentFace == DieFace.Heart);
        var availableHearts = heartCount - gameState.CurrentTurn.HealingRayHeartsSpent;
        if (command.HealingAmount > availableHearts)
        {
            throw new InvalidOperationException("Not enough unused heart dice for Healing Ray.");
        }

        return (healer, target, gameState.CurrentTurn);
    }
}
