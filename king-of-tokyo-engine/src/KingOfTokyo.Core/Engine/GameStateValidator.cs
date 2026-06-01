using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Engine;

public sealed class GameStateValidator
{
    public void EnsureCanInitializeGame(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (gameState.Status != GameStatus.Setup)
        {
            throw new InvalidOperationException("Game can only be initialized from setup state.");
        }
    }

    public void EnsureCanBeginTurn(GameState gameState, BeginTurnCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot begin turn when game is not running.");
        }

        if (gameState.CurrentTurn is not null && gameState.CurrentTurn.Phase != TurnPhase.Finished)
        {
            throw new InvalidOperationException("Cannot begin a new turn while another turn is still active.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (!currentPlayer.IsAlive)
        {
            throw new InvalidOperationException("Current player is dead and cannot begin a turn.");
        }

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    public void EnsureCanRollDice(GameState gameState, RollDiceCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot roll dice when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot roll dice without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot roll dice while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling)
        {
            throw new InvalidOperationException("Dice can only be rolled during the rolling phase.");
        }

        if (gameState.CurrentTurn.RollCountUsed != 0)
        {
            throw new InvalidOperationException("Initial roll has already been used.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    public void EnsureCanRerollDice(GameState gameState, RerollDiceCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot reroll dice when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot reroll dice without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            if (gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll)
            {
                throw new InvalidOperationException("Cannot reroll dice while another decision is pending.");
            }

            if (gameState.PendingDecision.PlayerId != gameState.CurrentTurn.CurrentPlayerId)
            {
                throw new InvalidOperationException("Reroll decision belongs to a different player.");
            }
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling)
        {
            throw new InvalidOperationException("Dice can only be rerolled during the rolling phase.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Cannot reroll before the first roll.");
        }

        if (gameState.CurrentTurn.RollCountUsed >= gameState.CurrentTurn.MaxRolls)
        {
            throw new InvalidOperationException("No rerolls remain.");
        }

        if (command.DiceIndexesToReroll.Count == 0)
        {
            throw new InvalidOperationException("At least one die must be selected for reroll.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    public void EnsureCanFinalizeDice(GameState gameState, FinalizeDiceCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot finalize dice when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot finalize dice without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            if (gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll)
            {
                throw new InvalidOperationException("Cannot finalize dice while another decision is pending.");
            }

            if (gameState.PendingDecision.PlayerId != gameState.CurrentTurn.CurrentPlayerId)
            {
                throw new InvalidOperationException("Finalize decision belongs to a different player.");
            }
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling)
        {
            throw new InvalidOperationException("Dice can only be finalized during the rolling phase.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Cannot finalize dice before the first roll.");
        }

        if (gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Dice have already been finalized.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    public void EnsureCanChooseLeaveTokyo(GameState gameState, ChooseLeaveTokyoCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot choose Tokyo leave when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot choose Tokyo leave without an active turn.");
        }

        if (gameState.PendingDecision is null)
        {
            throw new InvalidOperationException("There is no pending decision.");
        }

        if (gameState.PendingDecision.DecisionType != DecisionType.LeaveTokyo)
        {
            throw new InvalidOperationException("Current pending decision is not a Tokyo leave decision.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("ChooseLeaveTokyoCommand requires an actor player id.");
        }

        if (gameState.PendingDecision.PlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Actor does not match the pending Tokyo defender.");
        }

        if (!gameState.CurrentTurn.HasPendingTokyoLeaveDecisions)
        {
            throw new InvalidOperationException("There is no queued Tokyo leave decision context.");
        }
    }

    public void EnsureCanEndTurn(GameState gameState, EndTurnCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot end turn when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot end turn without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot end turn while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
        {
            throw new InvalidOperationException("Turn can only be ended from the purchase phase.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    public void EnsureCanAdvanceToNextPlayer(GameState gameState, AdvanceToNextPlayerCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot advance to next player when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot advance to next player without a completed turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot advance to next player while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Finished)
        {
            throw new InvalidOperationException("Can only advance after the current turn is finished.");
        }
    }

    public void EnsureCanBuyFaceUpCard(GameState gameState, BuyFaceUpCardCommand command, int effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot buy cards when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot buy cards without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot buy cards while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
        {
            throw new InvalidOperationException("Cards can only be bought during the purchase phase.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (command.SlotIndex < 0 || command.SlotIndex >= gameState.Market.FaceUpCards.Count)
        {
            throw new InvalidOperationException("Selected market slot is invalid.");
        }

        var card = gameState.Market.FaceUpCards[command.SlotIndex];
        if (card is null)
        {
            throw new InvalidOperationException("Selected market slot is empty.");
        }

        if (currentPlayer.Energy < effectiveCost)
        {
            throw new InvalidOperationException("Player does not have enough energy to buy this card.");
        }
    }

    public void EnsureCanRefreshMarket(GameState gameState, RefreshMarketCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot refresh market when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot refresh market without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot refresh market while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
        {
            throw new InvalidOperationException("Market can only be refreshed during the purchase phase.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (currentPlayer.Energy < Services.MarketRefreshService.RefreshCost)
        {
            throw new InvalidOperationException("Player does not have enough energy to refresh the market.");
        }

        if (gameState.Market.FaceUpCards.All(card => card is null))
        {
            throw new InvalidOperationException("Cannot refresh an empty market.");
        }
    }

    public void EnsureCanActivateRapidHealing(GameState gameState, ActivateRapidHealingCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

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

        var keepCardRulesService = new Services.KeepCardRulesService();

        if (!keepCardRulesService.CanUseRapidHealing(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Rapid Healing right now.");
        }
    }

    public void EnsureCanActivateTelepath(GameState gameState, ActivateTelepathCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Telepath when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Telepath without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Telepath can only be used during the rolling phase before dice are finalized.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Telepath can only be used after at least one roll.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        var keepCardRulesService = new Services.KeepCardRulesService();

        if (!keepCardRulesService.CanUseTelepath(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Telepath right now.");
        }
    }

    public void EnsureCanActivateStretchy(GameState gameState, ActivateStretchyCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        EnsureCanModifyDieDuringRolling(gameState, command.ActorPlayerId, command.DieIndex, "Stretchy");

        var currentPlayer = gameState.GetCurrentPlayer();
        var keepCardRulesService = new Services.KeepCardRulesService();

        if (!keepCardRulesService.CanUseStretchy(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Stretchy right now.");
        }
    }

    public void EnsureCanActivateHerdCuller(GameState gameState, ActivateHerdCullerCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        EnsureCanModifyDieDuringRolling(gameState, command.ActorPlayerId, command.DieIndex, "Herd Culler");

        if (gameState.CurrentTurn!.Flags.HerdCullerUsed)
        {
            throw new InvalidOperationException("Herd Culler can only be used once per turn.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();
        var keepCardRulesService = new Services.KeepCardRulesService();

        if (!keepCardRulesService.CanUseHerdCuller(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Herd Culler right now.");
        }
    }

    public void EnsureCanPeekTopDeckCard(GameState gameState, PeekTopDeckCardCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot peek top deck card when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot peek top deck card without an active turn.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot peek top deck card while another decision is pending.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
        {
            throw new InvalidOperationException("Top deck card can only be peeked during the purchase phase.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        var keepCardRulesService = new Services.KeepCardRulesService();

        if (!keepCardRulesService.CanUseMadeInALab(currentPlayer))
        {
            throw new InvalidOperationException("Player cannot use Made in a Lab.");
        }

        if (gameState.Market.DrawPileCount <= 0)
        {
            throw new InvalidOperationException("Market draw pile is empty.");
        }
    }

    public void EnsureCanBuyPeekedTopDeckCard(GameState gameState, BuyPeekedTopDeckCardCommand command, int effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot buy peeked top deck card when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot buy peeked top deck card without an active turn.");
        }

        if (gameState.PendingDecision is null || gameState.PendingDecision.DecisionType != DecisionType.PeekTopDeckCardPurchase)
        {
            throw new InvalidOperationException("No peeked top deck card is waiting for resolution.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (currentPlayer.Energy < effectiveCost)
        {
            throw new InvalidOperationException("Player does not have enough energy to buy the peeked card.");
        }

        if (gameState.Market.DrawPileCount <= 0)
        {
            throw new InvalidOperationException("Market draw pile is empty.");
        }
    }

    public void EnsureCanDeclinePeekedTopDeckCard(GameState gameState, DeclinePeekedTopDeckCardCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot decline peeked top deck card when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot decline peeked top deck card without an active turn.");
        }

        if (gameState.PendingDecision is null || gameState.PendingDecision.DecisionType != DecisionType.PeekTopDeckCardPurchase)
        {
            throw new InvalidOperationException("No peeked top deck card is waiting for resolution.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (command.ActorPlayerId.HasValue && command.ActorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }
    }

    private static void EnsureCanModifyDieDuringRolling(GameState gameState, int? actorPlayerId, int dieIndex, string cardName)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException($"Cannot activate {cardName} when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException($"Cannot activate {cardName} without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException($"{cardName} can only be used during the rolling phase before dice are finalized.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException($"{cardName} can only be used after at least one roll.");
        }

        var currentPlayer = gameState.GetCurrentPlayer();

        if (actorPlayerId.HasValue && actorPlayerId.Value != currentPlayer.PlayerId)
        {
            throw new InvalidOperationException("Actor does not match the current player.");
        }

        if (dieIndex < 0 || dieIndex >= gameState.CurrentTurn.DicePool.Dice.Count)
        {
            throw new InvalidOperationException("Selected die index is invalid.");
        }
    }
}