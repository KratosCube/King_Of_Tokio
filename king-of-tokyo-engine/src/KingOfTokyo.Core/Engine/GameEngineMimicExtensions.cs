using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public static class GameEngineMimicExtensions
{
    public static CommandResult Execute(this GameEngine engine, GameState gameState, SetMimicTargetCommand command)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var (mimicOwner, mimicCard, shouldPayRetargetCost) = EnsureCanSetMimicTarget(gameState, command);

            var mimicService = new MimicService();
            mimicService.SetTarget(
                gameState,
                mimicOwner.PlayerId,
                command.TargetOwnerPlayerId,
                command.TargetCardId);

            if (shouldPayRetargetCost)
            {
                mimicOwner.SpendEnergy(1);
            }

            return CommandResult.Successful(gameState);
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(gameState, ex.Message);
        }
    }

    private static (PlayerState MimicOwner, MarketCardState MimicCard, bool ShouldPayRetargetCost) EnsureCanSetMimicTarget(
        GameState gameState,
        SetMimicTargetCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot set Mimic target when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot set Mimic target without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot set Mimic target while another decision is pending.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("SetMimicTargetCommand requires an actor player id.");
        }

        if (gameState.CurrentTurn.CurrentPlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Only the current player can set Mimic target.");
        }

        var mimicOwner = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!mimicOwner.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot use Mimic.");
        }

        var mimicCard = mimicOwner.KeepCards.FirstOrDefault(card => card.CardId == KnownCardIds.Mimic)
            ?? throw new InvalidOperationException("Player does not have Mimic.");

        var isInitialTarget = mimicCard.MimicTarget is null;
        if (isInitialTarget)
        {
            if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
            {
                throw new InvalidOperationException("Initial Mimic target can only be set during the purchase phase.");
            }

            return (mimicOwner, mimicCard, ShouldPayRetargetCost: false);
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.RollCountUsed > 0 || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Mimic target can only be changed at the start of the owner's turn before rolling dice.");
        }

        if (mimicOwner.Energy < 1)
        {
            throw new InvalidOperationException("Player does not have enough energy to retarget Mimic.");
        }

        return (mimicOwner, mimicCard, ShouldPayRetargetCost: true);
    }
}
