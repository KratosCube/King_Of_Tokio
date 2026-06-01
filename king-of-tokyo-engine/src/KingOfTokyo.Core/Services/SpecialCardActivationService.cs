using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class SpecialCardActivationService
{
    private readonly KeepCardRulesService _keepCardRulesService;

    public SpecialCardActivationService(KeepCardRulesService? keepCardRulesService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public EngineStepResult ActivateTelepath(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot activate Telepath without an active turn.");

        var player = gameState.GetCurrentPlayer();
        player.SpendEnergy(1);
        currentTurn.AddExtraRolls(1);

        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(Array.Empty<GameEventBase>(), pendingDecision);
    }

    public EngineStepResult ActivateStretchy(GameState gameState, int dieIndex, DieFace targetFace)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot activate Stretchy without an active turn.");

        var player = gameState.GetCurrentPlayer();
        player.SpendEnergy(2);

        var die = currentTurn.DicePool.Dice.FirstOrDefault(d => d.Index == dieIndex)
            ?? throw new InvalidOperationException("Selected die does not exist.");

        die.SetFace(targetFace);

        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(Array.Empty<GameEventBase>(), pendingDecision);
    }

    public EngineStepResult ActivateHerdCuller(GameState gameState, int dieIndex)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot activate Herd Culler without an active turn.");

        var die = currentTurn.DicePool.Dice.FirstOrDefault(d => d.Index == dieIndex)
            ?? throw new InvalidOperationException("Selected die does not exist.");

        die.SetFace(DieFace.One);
        currentTurn.Flags.HerdCullerUsed = true;

        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(Array.Empty<GameEventBase>(), pendingDecision);
    }

    public EngineStepResult ActivateWings(GameState gameState, int playerId)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot activate Wings without an active turn.");

        var player = gameState.GetPlayerById(playerId);
        var damageTakenThisTurn = currentTurn.GetDamageTakenThisTurn(player.PlayerId);
        var cancelableDamage = Math.Min(damageTakenThisTurn, player.MaxHealth - player.Health);

        if (cancelableDamage <= 0)
        {
            throw new InvalidOperationException("Player has no damage left to cancel.");
        }

        player.SpendEnergy(KeepCardRulesService.WingsCost);
        player.Heal(cancelableDamage);
        currentTurn.ClearDamageTakenThisTurn(player.PlayerId);
        currentTurn.SetPendingTokyoLeaveDamageTaken(player.PlayerId, 0);

        var events = new GameEventBase[]
        {
            new DamageCanceledEvent(
                player.PlayerId,
                cancelableDamage,
                "Keep card: Wings.")
        };

        return new EngineStepResult(events, gameState.PendingDecision);
    }

    public EngineStepResult ActivatePlotTwist(GameState gameState, int dieIndex, DieFace targetFace)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Plot Twist when game is not running.");
        }

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot activate Plot Twist without an active turn.");

        if (currentTurn.Phase != TurnPhase.Rolling || currentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Plot Twist can only be used during the rolling phase before dice are finalized.");
        }

        if (currentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Plot Twist can only be used after at least one roll.");
        }

        if (gameState.PendingDecision is not null &&
            gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll)
        {
            throw new InvalidOperationException("Cannot activate Plot Twist while another decision is pending.");
        }

        var player = gameState.GetCurrentPlayer();
        if (!player.HasKeepCard(KnownCardIds.PlotTwist))
        {
            throw new InvalidOperationException("Player cannot use Plot Twist right now.");
        }

        var die = currentTurn.DicePool.Dice.FirstOrDefault(d => d.Index == dieIndex)
            ?? throw new InvalidOperationException("Selected die does not exist.");

        die.SetFace(targetFace);

        var card = player.RemoveKeepCard(KnownCardIds.PlotTwist);
        gameState.Market.Discard(card);

        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);

        var events = new GameEventBase[]
        {
            new KeepCardDiscardedEvent(
                player.PlayerId,
                card.CardId,
                card.Name,
                "Keep card: Plot Twist.")
        };

        return new EngineStepResult(events, pendingDecision);
    }

    public EngineStepResult ActivateMetamorph(GameState gameState, string cardIdToDiscard)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var player = gameState.GetCurrentPlayer();
        var card = player.RemoveKeepCard(cardIdToDiscard);

        ApplyKeepCardLostEffect(player, card);
        player.GainEnergy(card.Cost);
        gameState.Market.Discard(card);

        var events = new GameEventBase[]
        {
            new KeepCardDiscardedEvent(
                player.PlayerId,
                card.CardId,
                card.Name,
                "Keep card: Metamorph."),
            new EnergyGainedEvent(
                player.PlayerId,
                card.Cost,
                "Keep card: Metamorph.")
        };

        return new EngineStepResult(events);
    }

    public EngineStepResult PeekTopDeckCard(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var player = gameState.GetCurrentPlayer();
        var topCard = gameState.Market.PeekTopDrawCard();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(player, topCard);

        var pendingDecision = new PendingDecision
        {
            DecisionType = DecisionType.PeekTopDeckCardPurchase,
            PlayerId = player.PlayerId,
            Payload = new PeekTopDeckCardDecisionData
            {
                CardId = topCard.CardId,
                CardName = topCard.Name,
                Description = topCard.Description,
                BaseCost = topCard.Cost,
                EffectiveCost = effectiveCost,
                CardType = topCard.CardType
            }
        };

        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(Array.Empty<GameEventBase>(), pendingDecision);
    }

    public EngineStepResult DeclinePeekedTopDeckCard(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        gameState.ClearPendingDecision();

        return EngineStepResult.Empty;
    }

    private static void ApplyKeepCardLostEffect(PlayerState player, MarketCardState card)
    {
        if (card.CardId == KnownCardIds.EvenBigger)
        {
            player.DecreaseMaxHealth(2);
        }
    }

    private static PendingDecision? CreateRerollDecisionIfAvailable(TurnState currentTurn)
    {
        if (currentTurn.RollCountUsed >= currentTurn.MaxRolls)
        {
            return null;
        }

        return new PendingDecision
        {
            DecisionType = DecisionType.SelectDiceToReroll,
            PlayerId = currentTurn.CurrentPlayerId,
            Payload = new RerollDecisionData
            {
                CurrentLockedDiceIndexes = currentTurn.DicePool.Dice
                    .Where(d => d.IsLocked)
                    .Select(d => d.Index)
                    .ToArray(),
                CurrentFaces = currentTurn.DicePool.Dice
                    .Select(d => d.CurrentFace)
                    .ToArray(),
                RollCountUsed = currentTurn.RollCountUsed,
                MaxRolls = currentTurn.MaxRolls
            }
        };
    }
}
