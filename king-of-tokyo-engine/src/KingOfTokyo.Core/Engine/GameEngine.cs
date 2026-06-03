using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Rules.Victory;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public sealed class GameEngine
{
    private readonly DiceRollService _diceRollService;
    private readonly DiceScoringService _diceScoringService;
    private readonly DamageResolver _damageResolver;
    private readonly TokyoController _tokyoController;
    private readonly TurnLifecycleService _turnLifecycleService;
    private readonly MarketSetupService _marketSetupService;
    private readonly MarketPurchaseService _marketPurchaseService;
    private readonly MarketRefreshService _marketRefreshService;
    private readonly OwnedCardTransferService _ownedCardTransferService;
    private readonly RapidHealingService _rapidHealingService;
    private readonly KeepCardRulesService _keepCardRulesService;
    private readonly SpecialCardActivationService _specialCardActivationService;
    private readonly EliminationService _eliminationService;
    private readonly GameStateValidator _validator;
    private readonly List<GameEventBase> _eventLog = new();

    public GameEngine(
        DiceRollService? diceRollService = null,
        DiceScoringService? diceScoringService = null,
        DamageResolver? damageResolver = null,
        TokyoController? tokyoController = null,
        TurnLifecycleService? turnLifecycleService = null,
        MarketSetupService? marketSetupService = null,
        MarketPurchaseService? marketPurchaseService = null,
        MarketRefreshService? marketRefreshService = null,
        OwnedCardTransferService? ownedCardTransferService = null,
        RapidHealingService? rapidHealingService = null,
        KeepCardRulesService? keepCardRulesService = null,
        SpecialCardActivationService? specialCardActivationService = null,
        EliminationService? eliminationService = null,
        GameStateValidator? validator = null)
    {
        _diceRollService = diceRollService ?? new DiceRollService();
        _diceScoringService = diceScoringService ?? new DiceScoringService();
        _damageResolver = damageResolver ?? new DamageResolver();
        _tokyoController = tokyoController ?? new TokyoController();
        _turnLifecycleService = turnLifecycleService ?? new TurnLifecycleService(_diceRollService, _diceScoringService, _damageResolver, _tokyoController, _keepCardRulesService);
        _marketSetupService = marketSetupService ?? new MarketSetupService();
        _marketPurchaseService = marketPurchaseService ?? new MarketPurchaseService();
        _marketRefreshService = marketRefreshService ?? new MarketRefreshService();
        _ownedCardTransferService = ownedCardTransferService ?? new OwnedCardTransferService();
        _rapidHealingService = rapidHealingService ?? new RapidHealingService();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _specialCardActivationService = specialCardActivationService ?? new SpecialCardActivationService(_keepCardRulesService);
        _eliminationService = eliminationService ?? new EliminationService();
        _validator = validator ?? new GameStateValidator();
    }

    public IReadOnlyList<GameEventBase> EventLog => _eventLog;

    public CommandResult Execute(GameState gameState, IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var result = command switch
            {
                InitializeGameCommand initialize => ExecuteInitialize(gameState, initialize),
                BeginTurnCommand beginTurn => ExecuteBeginTurn(gameState, beginTurn),
                RollDiceCommand rollDice => ExecuteRollDice(gameState, rollDice),
                RerollDiceCommand rerollDice => ExecuteRerollDice(gameState, rerollDice),
                FinalizeDiceCommand finalizeDice => ExecuteFinalizeDice(gameState, finalizeDice),
                ResolveTokyoEntryCommand resolveTokyoEntry => ExecuteResolveTokyoEntry(gameState, resolveTokyoEntry),
                BuyCardCommand buyCard => ExecuteBuyCard(gameState, buyCard),
                BuyOwnedKeepCardCommand buyOwnedKeepCard => ExecuteBuyOwnedKeepCard(gameState, buyOwnedKeepCard),
                RefreshMarketCommand refreshMarket => ExecuteRefreshMarket(gameState, refreshMarket),
                ActivateRapidHealingCommand rapidHealing => ExecuteActivateRapidHealing(gameState, rapidHealing),
                ActivateTelepathCommand telepath => ExecuteActivateTelepath(gameState, telepath),
                ActivateStretchyCommand stretchy => ExecuteActivateStretchy(gameState, stretchy),
                ActivateHerdCullerCommand herdCuller => ExecuteActivateHerdCuller(gameState, herdCuller),
                ActivateSmokeCloudCommand smokeCloud => ExecuteActivateSmokeCloud(gameState, smokeCloud),
                ActivatePsychicProbeCommand psychicProbe => ExecuteActivatePsychicProbe(gameState, psychicProbe),
                ActivateWingsCommand wings => ExecuteActivateWings(gameState, wings),
                ActivatePlotTwistCommand plotTwist => ExecuteActivatePlotTwist(gameState, plotTwist),
                ActivateMetamorphCommand metamorph => ExecuteActivateMetamorph(gameState, metamorph),
                PeekTopDeckCardCommand peekTopDeck => ExecutePeekTopDeckCard(gameState, peekTopDeck),
                BuyPeekedTopDeckCardCommand buyPeekedTopDeck => ExecuteBuyPeekedTopDeckCard(gameState, buyPeekedTopDeck),
                DeclinePeekedTopDeckCardCommand declinePeekedTopDeck => ExecuteDeclinePeekedTopDeckCard(gameState, declinePeekedTopDeck),
                BuyOpportunistRevealedCardCommand buyOpportunist => ExecuteBuyOpportunistRevealedCard(gameState, buyOpportunist),
                DeclineOpportunistRevealedCardCommand declineOpportunist => ExecuteDeclineOpportunistRevealedCard(gameState, declineOpportunist),
                EndTurnCommand endTurn => ExecuteEndTurn(gameState, endTurn),
                AdvanceToNextPlayerCommand advance => ExecuteAdvanceToNextPlayer(gameState, advance),
                _ => throw new NotSupportedException($"Command '{command.GetType().Name}' is not supported yet.")
            };

            gameState.IncrementVersion();
            return result;
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(gameState, ex.Message);
        }
    }

    private CommandResult ExecuteInitialize(GameState gameState, InitializeGameCommand command)
    {
        _validator.EnsureCanInitialize(gameState, command);
        _marketSetupService.InitializeMarket(gameState);
        gameState.StartGame();
        return CommandResult.Successful(gameState);
    }

    private CommandResult ExecuteBeginTurn(GameState gameState, BeginTurnCommand command)
    {
        _validator.EnsureCanBeginTurn(gameState, command);
        var currentPlayer = gameState.GetCurrentPlayer();
        var diceCount = _keepCardRulesService.GetEffectiveDiceCount(currentPlayer);
        var maxRolls = TurnState.DefaultMaxRolls + _keepCardRulesService.GetExtraRerolls(currentPlayer);
        gameState.StartTurnForCurrentPlayer(diceCount, maxRolls);
        var stepResult = _turnLifecycleService.ApplyStartTurnEffects(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteRollDice(GameState gameState, RollDiceCommand command)
    {
        _validator.EnsureCanRollDice(gameState, command);
        _diceRollService.RollAll(gameState.CurrentTurn!.DicePool);
        gameState.CurrentTurn.IncrementRollCount();
        var faces = gameState.CurrentTurn.DicePool.Dice.Select(die => die.CurrentFace).ToArray();
        var events = new[] { new DiceRolledEvent(gameState.CurrentTurn.CurrentPlayerId, gameState.CurrentTurn.RollCountUsed, faces) };
        var pendingDecision = CreateRerollDecisionIfAvailable(gameState.CurrentTurn);
        gameState.SetPendingDecision(pendingDecision);
        PublishEvents(events);
        return CommandResult.Successful(gameState, events, pendingDecision);
    }

    private CommandResult ExecuteRerollDice(GameState gameState, RerollDiceCommand command)
    {
        _validator.EnsureCanRerollDice(gameState, command);
        _diceRollService.RerollSelected(gameState.CurrentTurn!.DicePool, command.DiceIndices);
        gameState.ClearPendingDecision();
        gameState.CurrentTurn.IncrementRollCount();
        var faces = gameState.CurrentTurn.DicePool.Dice.Select(die => die.CurrentFace).ToArray();
        var events = new[] { new DiceRolledEvent(gameState.CurrentTurn.CurrentPlayerId, gameState.CurrentTurn.RollCountUsed, faces) };
        var pendingDecision = CreateRerollDecisionIfAvailable(gameState.CurrentTurn);
        gameState.SetPendingDecision(pendingDecision);
        PublishEvents(events);
        return CommandResult.Successful(gameState, events, pendingDecision);
    }

    private CommandResult ExecuteFinalizeDice(GameState gameState, FinalizeDiceCommand command)
    {
        _validator.EnsureCanFinalizeDice(gameState, command);
        var stepResult = _turnLifecycleService.FinalizeDice(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteResolveTokyoEntry(GameState gameState, ResolveTokyoEntryCommand command)
    {
        _validator.EnsureCanResolveTokyoEntry(gameState, command);
        gameState.Tokyo.EnterTokyo(command.PlayerId, gameState.Players.Count);
        return CommandResult.Successful(gameState);
    }

    private CommandResult ExecuteBuyCard(GameState gameState, BuyCardCommand command)
    {
        var card = gameState.Market.FaceUpCards[command.SlotIndex]
            ?? throw new InvalidOperationException("Selected market slot is empty.");
        var currentPlayer = gameState.GetCurrentPlayer();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(currentPlayer, card);
        _validator.EnsureCanBuyFaceUpCard(gameState, command, effectiveCost);
        var stepResult = _marketPurchaseService.BuyFaceUpCard(gameState, command.SlotIndex, effectiveCost);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteBuyOwnedKeepCard(GameState gameState, BuyOwnedKeepCardCommand command)
    {
        var (buyer, seller, card) = EnsureCanBuyOwnedKeepCard(gameState, command);
        _ownedCardTransferService.BuyKeepCardFromPlayer(gameState, buyer, seller, card.CardId, card.Cost);
        gameState.CurrentTurn!.Flags.BoughtCard = true;
        return CommandResult.Successful(gameState);
    }

    private CommandResult ExecuteRefreshMarket(GameState gameState, RefreshMarketCommand command)
    {
        _validator.EnsureCanRefreshMarket(gameState, command);
        var stepResult = _marketRefreshService.Refresh(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateRapidHealing(GameState gameState, ActivateRapidHealingCommand command)
    {
        _validator.EnsureCanActivateRapidHealing(gameState, command);
        var stepResult = _rapidHealingService.Activate(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateTelepath(GameState gameState, ActivateTelepathCommand command)
    {
        _validator.EnsureCanActivateTelepath(gameState, command);
        var stepResult = _specialCardActivationService.ActivateTelepath(gameState);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateStretchy(GameState gameState, ActivateStretchyCommand command)
    {
        _validator.EnsureCanActivateStretchy(gameState, command);
        var stepResult = _specialCardActivationService.ActivateStretchy(gameState, command.DieIndex, command.TargetFace);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateHerdCuller(GameState gameState, ActivateHerdCullerCommand command)
    {
        _validator.EnsureCanActivateHerdCuller(gameState, command);
        var stepResult = _specialCardActivationService.ActivateHerdCuller(gameState, command.DieIndex);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateSmokeCloud(GameState gameState, ActivateSmokeCloudCommand command)
    {
        _validator.EnsureCanActivateSmokeCloud(gameState, command);
        var stepResult = _specialCardActivationService.ActivateSmokeCloud(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivatePsychicProbe(GameState gameState, ActivatePsychicProbeCommand command)
    {
        EnsureCanActivatePsychicProbe(gameState, command);

        var currentTurn = gameState.CurrentTurn!;
        var actor = gameState.GetPlayerById(command.ActorPlayerId!.Value);
        var psychicProbe = actor.KeepCards.Single(card => card.CardId == KnownCardIds.PsychicProbe);
        psychicProbe.AddCounters(1);

        _diceRollService.RerollSelected(currentTurn.DicePool, new[] { command.TargetDieIndex });
        var rerolledFace = currentTurn.DicePool.Dice[command.TargetDieIndex].CurrentFace;

        var events = new List<GameEventBase>
        {
            new DiceRolledEvent(currentTurn.CurrentPlayerId, currentTurn.RollCountUsed, currentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray())
        };

        if (rerolledFace == DieFace.Energy)
        {
            var discardedCard = actor.RemoveKeepCard(KnownCardIds.PsychicProbe);
            gameState.Market.Discard(discardedCard);
            events.Add(new KeepCardDiscardedEvent(
                actor.PlayerId,
                discardedCard.CardId,
                discardedCard.Name,
                "Keep card: Psychic Probe."));
        }

        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);
        PublishEvents(events);
        return CommandResult.Successful(gameState, events, pendingDecision);
    }

    private CommandResult ExecuteActivateWings(GameState gameState, ActivateWingsCommand command)
    {
        _validator.EnsureCanActivateWings(gameState, command);
        var stepResult = _specialCardActivationService.ActivateWings(gameState, command.ActorPlayerId!.Value);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivatePlotTwist(GameState gameState, ActivatePlotTwistCommand command)
    {
        _validator.EnsureCanActivatePlotTwist(gameState, command);
        var stepResult = _specialCardActivationService.ActivatePlotTwist(gameState, command.DieIndex, command.TargetFace);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteActivateMetamorph(GameState gameState, ActivateMetamorphCommand command)
    {
        _validator.EnsureCanActivateMetamorph(gameState, command);
        var stepResult = _specialCardActivationService.ActivateMetamorph(gameState, command.CardIdToDiscard);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecutePeekTopDeckCard(GameState gameState, PeekTopDeckCardCommand command)
    {
        _validator.EnsureCanPeekTopDeckCard(gameState, command);
        var stepResult = _specialCardActivationService.PeekTopDeckCard(gameState);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteBuyPeekedTopDeckCard(GameState gameState, BuyPeekedTopDeckCardCommand command)
    {
        var topCard = gameState.Market.PeekTopDrawCard();
        var currentPlayer = gameState.GetCurrentPlayer();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(currentPlayer, topCard);
        _validator.EnsureCanBuyPeekedTopDeckCard(gameState, command, effectiveCost);
        var stepResult = _marketPurchaseService.BuyTopDeckCard(gameState, effectiveCost);
        gameState.ClearPendingDecision();
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, null);
    }

    private CommandResult ExecuteDeclinePeekedTopDeckCard(GameState gameState, DeclinePeekedTopDeckCardCommand command)
    {
        _validator.EnsureCanDeclinePeekedTopDeckCard(gameState, command);
        var stepResult = _specialCardActivationService.DeclinePeekedTopDeckCard(gameState);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteBuyOpportunistRevealedCard(GameState gameState, BuyOpportunistRevealedCardCommand command)
    {
        var payload = EnsureCanBuyOpportunistRevealedCard(gameState, command);
        var actor = gameState.GetPlayerById(command.ActorPlayerId!.Value);
        var card = gameState.Market.FaceUpCards[payload.SlotIndex]
            ?? throw new InvalidOperationException("Selected market slot is empty.");
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(actor, card);
        var stepResult = _marketPurchaseService.BuyOpportunistRevealedCard(
            gameState,
            actor.PlayerId,
            payload.SlotIndex,
            effectiveCost);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteDeclineOpportunistRevealedCard(GameState gameState, DeclineOpportunistRevealedCardCommand command)
    {
        EnsureCanDeclineOpportunistRevealedCard(gameState, command);
        gameState.ClearPendingDecision();
        return CommandResult.Successful(gameState);
    }

    private CommandResult ExecuteEndTurn(GameState gameState, EndTurnCommand command)
    {
        _validator.EnsureCanEndTurn(gameState, command);
        var stepResult = _turnLifecycleService.EndTurn(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteAdvanceToNextPlayer(GameState gameState, AdvanceToNextPlayerCommand command)
    {
        _validator.EnsureCanAdvanceToNextPlayer(gameState, command);
        _turnLifecycleService.AdvanceToNextPlayer(gameState);
        return CommandResult.Successful(gameState);
    }

    private static (KingOfTokyo.Core.Domain.Entities.PlayerState Buyer, KingOfTokyo.Core.Domain.Entities.PlayerState Seller, KingOfTokyo.Core.Domain.Entities.MarketCardState Card) EnsureCanBuyOwnedKeepCard(GameState gameState, BuyOwnedKeepCardCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot buy owned keep card when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot buy owned keep card without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Purchase)
        {
            throw new InvalidOperationException("Owned keep cards can only be bought during the purchase phase.");
        }

        if (gameState.PendingDecision is not null)
        {
            throw new InvalidOperationException("Cannot buy owned keep card while a decision is pending.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("BuyOwnedKeepCardCommand requires an actor player id.");
        }

        if (gameState.CurrentTurn.CurrentPlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Only the current player can buy owned keep cards.");
        }

        var buyer = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!buyer.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot buy owned keep cards.");
        }

        if (!buyer.HasKeepCard(KnownCardIds.ParasiticTentacles))
        {
            throw new InvalidOperationException("Player does not have Parasitic Tentacles.");
        }

        var seller = gameState.GetPlayerById(command.SellerPlayerId);
        if (!seller.IsAlive)
        {
            throw new InvalidOperationException("Cannot buy cards from dead players.");
        }

        if (buyer.PlayerId == seller.PlayerId)
        {
            throw new InvalidOperationException("A player cannot buy a card from themselves.");
        }

        var card = seller.KeepCards.SingleOrDefault(card => card.CardId == command.CardId)
            ?? throw new InvalidOperationException("Seller does not own this keep card.");

        if (buyer.Energy < card.Cost)
        {
            throw new InvalidOperationException("Buyer does not have enough energy.");
        }

        return (buyer, seller, card);
    }

    private static MarketCardRevealDecisionData EnsureCanBuyOpportunistRevealedCard(GameState gameState, BuyOpportunistRevealedCardCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot buy Opportunist card when game is not running.");
        }

        if (gameState.PendingDecision is null)
        {
            throw new InvalidOperationException("There is no pending decision.");
        }

        if (gameState.PendingDecision.DecisionType != DecisionType.OpportunistPurchase)
        {
            throw new InvalidOperationException("Current pending decision is not an Opportunist purchase decision.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("BuyOpportunistRevealedCardCommand requires an actor player id.");
        }

        if (gameState.PendingDecision.PlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Actor does not match the pending Opportunist player.");
        }

        var actor = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!actor.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot buy Opportunist cards.");
        }

        if (!actor.HasKeepCard(KnownCardIds.Opportunist))
        {
            throw new InvalidOperationException("Player does not have Opportunist.");
        }

        var payload = gameState.PendingDecision.Payload as MarketCardRevealDecisionData
            ?? throw new InvalidOperationException("Opportunist purchase payload is invalid.");

        if (payload.SlotIndex < 0 || payload.SlotIndex >= gameState.Market.FaceUpCards.Count)
        {
            throw new InvalidOperationException("Selected market slot is invalid.");
        }

        var card = gameState.Market.FaceUpCards[payload.SlotIndex];
        if (card is null || card.CardId != payload.CardId)
        {
            throw new InvalidOperationException("Revealed market card is no longer available.");
        }

        var effectiveCost = new KeepCardRulesService().GetEffectivePurchaseCost(actor, card);
        var availableEnergy = new EnergyPaymentService().GetAvailableEnergy(actor);
        if (availableEnergy < effectiveCost)
        {
            throw new InvalidOperationException("Player does not have enough energy to buy this card.");
        }

        return payload;
    }

    private static void EnsureCanDeclineOpportunistRevealedCard(GameState gameState, DeclineOpportunistRevealedCardCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot decline Opportunist when game is not running.");
        }

        if (gameState.PendingDecision is null)
        {
            throw new InvalidOperationException("There is no pending decision.");
        }

        if (gameState.PendingDecision.DecisionType != DecisionType.OpportunistPurchase)
        {
            throw new InvalidOperationException("Current pending decision is not an Opportunist purchase decision.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("DeclineOpportunistRevealedCardCommand requires an actor player id.");
        }

        if (gameState.PendingDecision.PlayerId != command.ActorPlayerId.Value)
        {
            throw new InvalidOperationException("Actor does not match the pending Opportunist player.");
        }

        var actor = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!actor.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot decline Opportunist cards.");
        }

        if (!actor.HasKeepCard(KnownCardIds.Opportunist))
        {
            throw new InvalidOperationException("Player does not have Opportunist.");
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
                RollCountUsed = currentTurn.RollCountUsed,
                MaxRolls = currentTurn.MaxRolls,
                CurrentFaces = currentTurn.DicePool.Dice.Select(die => die.CurrentFace).ToArray()
            }
        };
    }

    private void PublishEvents(IEnumerable<GameEventBase> events)
    {
        foreach (var gameEvent in events)
        {
            _eventLog.Add(gameEvent);
            GameEventBus.Publish(gameEvent);
        }
    }
}
