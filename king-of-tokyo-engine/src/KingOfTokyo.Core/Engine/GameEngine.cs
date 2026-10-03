using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Engine;

public sealed class GameEngine : IGameEngine
{
    private readonly GameStateValidator _validator;
    private readonly DiceRollService _diceRollService;
    private readonly FinalizeDiceService _finalizeDiceService;
    private readonly TokyoDecisionService _tokyoDecisionService;
    private readonly TurnLifecycleService _turnLifecycleService;
    private readonly MarketSetupService _marketSetupService;
    private readonly MarketPurchaseService _marketPurchaseService;
    private readonly MarketRefreshService _marketRefreshService;
    private readonly RapidHealingService _rapidHealingService;
    private readonly SpecialCardActivationService _specialCardActivationService;
    private readonly KeepCardRulesService _keepCardRulesService;
    private readonly OwnedCardTransferService _ownedCardTransferService;
    private readonly IReadOnlyCollection<IGameObserver> _observers;

    public GameEngine(
        GameStateValidator? validator = null,
        DiceRollService? diceRollService = null,
        FinalizeDiceService? finalizeDiceService = null,
        TokyoDecisionService? tokyoDecisionService = null,
        TurnLifecycleService? turnLifecycleService = null,
        MarketSetupService? marketSetupService = null,
        MarketPurchaseService? marketPurchaseService = null,
        MarketRefreshService? marketRefreshService = null,
        RapidHealingService? rapidHealingService = null,
        SpecialCardActivationService? specialCardActivationService = null,
        KeepCardRulesService? keepCardRulesService = null,
        OwnedCardTransferService? ownedCardTransferService = null,
        IEnumerable<IGameObserver>? observers = null)
    {
        _validator = validator ?? new GameStateValidator();
        _diceRollService = diceRollService ?? new DiceRollService(new SystemRandomSource());
        _finalizeDiceService = finalizeDiceService ?? new FinalizeDiceService();
        _tokyoDecisionService = tokyoDecisionService ?? new TokyoDecisionService();
        _turnLifecycleService = turnLifecycleService ?? new TurnLifecycleService();
        _marketSetupService = marketSetupService ?? new MarketSetupService();
        _marketPurchaseService = marketPurchaseService ?? new MarketPurchaseService();
        _marketRefreshService = marketRefreshService ?? new MarketRefreshService();
        _rapidHealingService = rapidHealingService ?? new RapidHealingService();
        _specialCardActivationService = specialCardActivationService ?? new SpecialCardActivationService();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _ownedCardTransferService = ownedCardTransferService ?? new OwnedCardTransferService();
        _observers = (observers ?? Array.Empty<IGameObserver>()).ToArray();
    }

    public CommandResult Execute(GameState gameState, IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return command switch
            {
                InitializeGameCommand initializeGameCommand => ExecuteInitialize(gameState, initializeGameCommand),
                BeginTurnCommand beginTurnCommand => ExecuteBeginTurn(gameState, beginTurnCommand),
                RollDiceCommand rollDiceCommand => ExecuteRollDice(gameState, rollDiceCommand),
                RerollDiceCommand rerollDiceCommand => ExecuteRerollDice(gameState, rerollDiceCommand),
                RerollBackgroundDwellerThreesCommand backgroundDwellerCommand => ExecuteRerollBackgroundDwellerThrees(gameState, backgroundDwellerCommand),
                FinalizeDiceCommand finalizeDiceCommand => ExecuteFinalizeDice(gameState, finalizeDiceCommand),
                ContinueAfterRapidHealingCommand continueAfterRapidHealingCommand => ExecuteContinueAfterRapidHealing(gameState, continueAfterRapidHealingCommand),
                ChooseLeaveTokyoCommand chooseLeaveTokyoCommand => ExecuteChooseLeaveTokyo(gameState, chooseLeaveTokyoCommand),
                BuyFaceUpCardCommand buyFaceUpCardCommand => ExecuteBuyFaceUpCard(gameState, buyFaceUpCardCommand),
                BuyOwnedKeepCardCommand buyOwnedKeepCardCommand => ExecuteBuyOwnedKeepCard(gameState, buyOwnedKeepCardCommand),
                RefreshMarketCommand refreshMarketCommand => ExecuteRefreshMarket(gameState, refreshMarketCommand),
                ActivateRapidHealingCommand activateRapidHealingCommand => ExecuteActivateRapidHealing(gameState, activateRapidHealingCommand),
                ActivateHealingRayCommand activateHealingRayCommand => ExecuteActivateHealingRay(gameState, activateHealingRayCommand),
                ActivateTelepathCommand activateTelepathCommand => ExecuteActivateTelepath(gameState, activateTelepathCommand),
                ActivateStretchyCommand activateStretchyCommand => ExecuteActivateStretchy(gameState, activateStretchyCommand),
                ActivateHerdCullerCommand activateHerdCullerCommand => ExecuteActivateHerdCuller(gameState, activateHerdCullerCommand),
                ActivateSmokeCloudCommand activateSmokeCloudCommand => ExecuteActivateSmokeCloud(gameState, activateSmokeCloudCommand),
                ActivatePsychicProbeCommand activatePsychicProbeCommand => ExecuteActivatePsychicProbe(gameState, activatePsychicProbeCommand),
                ActivateWingsCommand activateWingsCommand => ExecuteActivateWings(gameState, activateWingsCommand),
                ActivatePlotTwistCommand activatePlotTwistCommand => ExecuteActivatePlotTwist(gameState, activatePlotTwistCommand),
                ActivateMetamorphCommand activateMetamorphCommand => ExecuteActivateMetamorph(gameState, activateMetamorphCommand),
                PeekTopDeckCardCommand peekTopDeckCardCommand => ExecutePeekTopDeckCard(gameState, peekTopDeckCardCommand),
                BuyPeekedTopDeckCardCommand buyPeekedTopDeckCardCommand => ExecuteBuyPeekedTopDeckCard(gameState, buyPeekedTopDeckCardCommand),
                DeclinePeekedTopDeckCardCommand declinePeekedTopDeckCardCommand => ExecuteDeclinePeekedTopDeckCard(gameState, declinePeekedTopDeckCardCommand),
                BuyOpportunistRevealedCardCommand buyOpportunistRevealedCardCommand => ExecuteBuyOpportunistRevealedCard(gameState, buyOpportunistRevealedCardCommand),
                DeclineOpportunistRevealedCardCommand declineOpportunistRevealedCardCommand => ExecuteDeclineOpportunistRevealedCard(gameState, declineOpportunistRevealedCardCommand),
                EndTurnCommand endTurnCommand => ExecuteEndTurn(gameState, endTurnCommand),
                AdvanceToNextPlayerCommand advanceToNextPlayerCommand => ExecuteAdvanceToNextPlayer(gameState, advanceToNextPlayerCommand),
                _ => throw new NotSupportedException($"Command '{command.GetType().Name}' is not supported yet.")
            };
        }
        catch (Exception ex)
        {
            return CommandResult.Failed(gameState, ex.Message);
        }
    }

    private CommandResult ExecuteInitialize(GameState gameState, InitializeGameCommand command)
    {
        _validator.EnsureCanInitializeGame(gameState);
        gameState.StartGame();
        _marketSetupService.InitializeMarket(gameState);
        return CommandResult.Successful(gameState);
    }

    private CommandResult ExecuteBeginTurn(GameState gameState, BeginTurnCommand command)
    {
        _validator.EnsureCanBeginTurn(gameState, command);
        ResetPsychicProbeUsage(gameState);
        var currentPlayer = gameState.GetCurrentPlayer();
        var scheduledTurn = gameState.ConsumeNextScheduledTurnForCurrentPlayer();
        var diceCountModifier = scheduledTurn?.DiceCountModifier ?? 0;
        var baseDiceCount = _keepCardRulesService.GetEffectiveDiceCount(currentPlayer);
        var diceCount = Math.Max(KeepCardRulesService.MinimumDiceCount, baseDiceCount + diceCountModifier);
        var maxRolls = 3 + _keepCardRulesService.GetExtraRerolls(currentPlayer);
        gameState.StartTurnForCurrentPlayer(
            diceCount: diceCount,
            maxRolls: maxRolls,
            isExtraTurn: scheduledTurn is not null,
            diceCountModifier: diceCountModifier);
        currentPlayer = gameState.GetCurrentPlayer();
        var newEvents = new List<GameEventBase> { new TurnStartedEvent(currentPlayer.PlayerId) };
        newEvents.AddRange(_turnLifecycleService.TransferMonsterBatteriesAtTurnStart(gameState));

        if (gameState.CurrentTurn!.Flags.StartedTurnInTokyo)
        {
            currentPlayer.GainVictoryPoints(2);
            gameState.CurrentTurn.Flags.ScoredVictoryPoints = true;
            newEvents.Add(new VictoryPointsGainedEvent(currentPlayer.PlayerId, 2, "Started turn in Tokyo."));

            var urbavoreBonus = _keepCardRulesService.GetBonusStartTurnTokyoVictoryPoints(currentPlayer);
            if (urbavoreBonus > 0)
            {
                currentPlayer.GainVictoryPoints(urbavoreBonus);
                newEvents.Add(new VictoryPointsGainedEvent(currentPlayer.PlayerId, urbavoreBonus, "Keep card: Urbavore."));
            }
        }

        gameState.CurrentTurn.SetPhase(TurnPhase.Rolling);
        gameState.ClearPendingDecision();
        PublishEvents(newEvents);
        return CommandResult.Successful(gameState, newEvents);
    }

    private CommandResult ExecuteRollDice(GameState gameState, RollDiceCommand command)
    {
        _validator.EnsureCanRollDice(gameState, command);
        var currentTurn = gameState.CurrentTurn!;
        var currentPlayer = gameState.GetCurrentPlayer();
        _diceRollService.RollAll(currentTurn.DicePool, currentPlayer);
        currentTurn.IncrementRollCount();
        var newEvents = new List<GameEventBase> { new DiceRolledEvent(currentPlayer.PlayerId, currentTurn.RollCountUsed, currentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray()) };
        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);
        PublishEvents(newEvents);
        return CommandResult.Successful(gameState, newEvents, pendingDecision);
    }

    private CommandResult ExecuteRerollDice(GameState gameState, RerollDiceCommand command)
    {
        _validator.EnsureCanRerollDice(gameState, command);
        var currentTurn = gameState.CurrentTurn!;
        var currentPlayer = gameState.GetCurrentPlayer();
        _diceRollService.RerollSelected(currentTurn.DicePool, command.DiceIndexesToReroll, currentPlayer);
        currentTurn.IncrementRollCount();
        var newEvents = new List<GameEventBase> { new DiceRolledEvent(currentPlayer.PlayerId, currentTurn.RollCountUsed, currentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray()) };
        var pendingDecision = CreateRerollDecisionIfAvailable(currentTurn);
        gameState.SetPendingDecision(pendingDecision);
        PublishEvents(newEvents);
        return CommandResult.Successful(gameState, newEvents, pendingDecision);
    }

    private CommandResult ExecuteFinalizeDice(GameState gameState, FinalizeDiceCommand command)
    {
        _validator.EnsureCanFinalizeDice(gameState, command);
        _finalizeDiceService.ValidateHealingRayReservation(gameState, command.HeartsReservedForHealingRay);

        var defenders = GetDefendersFacingLethalDamage(gameState);
        if (defenders.Count > 0)
        {
            var turn = gameState.CurrentTurn!;
            turn.StartRapidHealingWindow(defenders, command.HeartsReservedForHealingRay);
            var decision = CreateRapidHealingDecision(turn.NextRapidHealingDefenderId);
            gameState.SetPendingDecision(decision);
            return CommandResult.Successful(gameState, pendingDecision: decision);
        }

        var stepResult = _finalizeDiceService.Execute(gameState, command.HeartsReservedForHealingRay);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteContinueAfterRapidHealing(GameState gameState, ContinueAfterRapidHealingCommand command)
    {
        var turn = gameState.CurrentTurn;
        if (gameState.Status != GameStatus.Running || turn is null ||
            turn.Phase != (turn.EndTurnAfterRapidHealing || turn.PurchaseCommandAfterRapidHealing is not null
                ? TurnPhase.Purchase : TurnPhase.Rolling) ||
            !turn.HasPendingRapidHealingDefenders ||
            gameState.PendingDecision?.DecisionType != DecisionType.RapidHealingBeforeDamage ||
            command.ActorPlayerId != turn.NextRapidHealingDefenderId ||
            gameState.PendingDecision.PlayerId != command.ActorPlayerId)
        {
            throw new InvalidOperationException("Only the pending defender can continue damage resolution.");
        }

        turn.ContinueAfterRapidHealing();
        if (turn.HasPendingRapidHealingDefenders)
        {
            var nextDecision = CreateRapidHealingDecision(turn.NextRapidHealingDefenderId);
            gameState.SetPendingDecision(nextDecision);
            return CommandResult.Successful(gameState, pendingDecision: nextDecision);
        }

        gameState.ClearPendingDecision();
        try
        {
            if (turn.PurchaseCommandAfterRapidHealing is { } purchaseCommand)
            {
                gameState.SetPendingDecision(turn.SuspendedPurchaseDecision);
                return purchaseCommand switch
                {
                    BuyFaceUpCardCommand faceUp => ExecuteBuyFaceUpCard(gameState, faceUp, allowRapidHealingWindow: false),
                    BuyPeekedTopDeckCardCommand peeked => ExecuteBuyPeekedTopDeckCard(gameState, peeked, allowRapidHealingWindow: false),
                    BuyOpportunistRevealedCardCommand opportunist => ExecuteBuyOpportunistRevealedCard(gameState, opportunist, allowRapidHealingWindow: false),
                    _ => throw new InvalidOperationException("Unsupported purchase after Rapid Healing.")
                };
            }

            if (turn.EndTurnAfterRapidHealing)
            {
                var endTurnCommand = new EndTurnCommand(turn.CurrentPlayerId);
                _validator.EnsureCanEndTurn(gameState, endTurnCommand);
                var endTurnResult = _turnLifecycleService.EndTurn(gameState);
                PublishEvents(endTurnResult.Events);
                return CommandResult.Successful(gameState, endTurnResult.Events, endTurnResult.PendingDecision);
            }

            var finalizeCommand = new FinalizeDiceCommand(turn.CurrentPlayerId, turn.HeartsReservedForHealingRayAfterRapidHealing);
            _validator.EnsureCanFinalizeDice(gameState, finalizeCommand);
            var stepResult = _finalizeDiceService.Execute(gameState, finalizeCommand.HeartsReservedForHealingRay);
            PublishEvents(stepResult.Events);
            return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
        }
        finally
        {
            turn.ClearWingsProtection();
        }
    }

    private IReadOnlyList<int> GetDefendersFacingLethalDamage(GameState gameState)
    {
        var attacker = gameState.GetCurrentPlayer();
        var summary = new DiceSummaryBuilder().Build(gameState.CurrentTurn!.DicePool);
        var projectedDamage = new Dictionary<int, int>();
        foreach (var packet in new AttackResolver(_keepCardRulesService).ResolveAttack(gameState, attacker, summary))
        {
            projectedDamage[packet.TargetPlayerId] = projectedDamage.GetValueOrDefault(packet.TargetPlayerId) + packet.Amount;
        }

        var poisonDamage = _keepCardRulesService.GetPoisonQuillsDamage(attacker, summary.OneCount);
        if (poisonDamage > 0)
        {
            poisonDamage += _keepCardRulesService.GetAcidAttackBonusDamage(attacker, poisonDamage);
            var poisonTargets = gameState.Players.Where(player => player.IsAlive && player.PlayerId != attacker.PlayerId &&
                (attacker.TokyoSlot == TokyoSlot.None ? player.TokyoSlot != TokyoSlot.None : player.TokyoSlot == TokyoSlot.None));
            foreach (var target in poisonTargets)
            {
                projectedDamage[target.PlayerId] = projectedDamage.GetValueOrDefault(target.PlayerId) + poisonDamage;
            }
        }

        return gameState.Players
            .Where(player => player.IsAlive && projectedDamage.GetValueOrDefault(player.PlayerId) >= player.Health &&
                CanPreventLethalDamage(player))
            .Select(player => player.PlayerId)
            .ToArray();
    }

    private bool CanPreventLethalDamage(PlayerState player) =>
        _keepCardRulesService.CanUseRapidHealing(player) || _keepCardRulesService.CanUseWings(player);

    private static PendingDecision CreateRapidHealingDecision(int defenderId) => new()
    {
        DecisionType = DecisionType.RapidHealingBeforeDamage,
        PlayerId = defenderId,
        Payload = new LethalDamageDecisionData()
    };

    private CommandResult? StartPurchaseRapidHealingWindow(
        GameState gameState,
        PlayerState buyer,
        MarketCardState card,
        IGameCommand command)
    {
        var effect = card.PurchaseEffect;
        if (effect.DamageAllOthers <= 0 && effect.DamageAllIncludingSelf <= 0 &&
            effect.DamageSelf <= 0 && effect.DamageOthersPerTwoEnergy <= 0)
        {
            return null;
        }

        var defenders = gameState.Players
            .Where(target => target.IsAlive && CanPreventLethalDamage(target) &&
                (target.PlayerId != buyer.PlayerId ||
                 target.Energy >= _keepCardRulesService.GetEffectivePurchaseCost(buyer, card) + 2))
            .Where(target =>
            {
                var isBuyer = target.PlayerId == buyer.PlayerId;
                var energyDamage = isBuyer ? 0 : (target.Energy / 2) * effect.DamageOthersPerTwoEnergy;
                var amounts = new[]
                {
                    isBuyer ? effect.DamageSelf : effect.DamageAllOthers,
                    effect.DamageAllIncludingSelf,
                    energyDamage
                };
                var projectedDamage = amounts.Where(amount => amount > 0)
                    .Sum(amount => amount + (isBuyer ? 0 : _keepCardRulesService.GetAcidAttackBonusDamage(buyer, amount)));
                return projectedDamage >= target.Health;
            })
            .Select(target => target.PlayerId)
            .ToArray();

        if (defenders.Length == 0)
        {
            return null;
        }

        var turn = gameState.CurrentTurn!;
        turn.StartRapidHealingWindow(defenders, 0, purchaseCommand: command,
            suspendedPurchaseDecision: gameState.PendingDecision,
            purchaseBuyerId: buyer.PlayerId,
            purchaseCost: _keepCardRulesService.GetEffectivePurchaseCost(buyer, card));
        var decision = CreateRapidHealingDecision(turn.NextRapidHealingDefenderId);
        gameState.SetPendingDecision(decision);
        return CommandResult.Successful(gameState, pendingDecision: decision);
    }

    private CommandResult ExecuteRerollBackgroundDwellerThrees(GameState gameState, RerollBackgroundDwellerThreesCommand command)
    {
        var turn = gameState.CurrentTurn;
        if (gameState.Status != GameStatus.Running || turn is null || turn.Phase != TurnPhase.Rolling ||
            turn.RollCountUsed == 0 || turn.DiceResolved || command.ActorPlayerId != turn.CurrentPlayerId ||
            !gameState.GetCurrentPlayer().HasKeepCard(KnownCardIds.BackgroundDweller) ||
            (gameState.PendingDecision is not null && gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll))
        {
            throw new InvalidOperationException("Background Dweller cannot be used now.");
        }

        _diceRollService.RerollBackgroundDwellerThrees(turn.DicePool, command.DiceIndexes);
        var events = new GameEventBase[]
        {
            new DiceRolledEvent(turn.CurrentPlayerId, turn.RollCountUsed, turn.DicePool.Dice.Select(die => die.CurrentFace).ToArray())
        };
        PublishEvents(events);
        return CommandResult.Successful(gameState, events, gameState.PendingDecision);
    }

    private CommandResult ExecuteChooseLeaveTokyo(GameState gameState, ChooseLeaveTokyoCommand command)
    {
        _validator.EnsureCanChooseLeaveTokyo(gameState, command);
        var stepResult = _tokyoDecisionService.Execute(gameState, command.LeaveTokyo);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteBuyFaceUpCard(GameState gameState, BuyFaceUpCardCommand command, bool allowRapidHealingWindow = true)
    {
        var card = gameState.Market.FaceUpCards[command.SlotIndex] ?? throw new InvalidOperationException("Selected market slot is empty.");
        var currentPlayer = gameState.GetCurrentPlayer();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(currentPlayer, card);
        _validator.EnsureCanBuyFaceUpCard(gameState, command, effectiveCost);
        if (allowRapidHealingWindow && command.StoredEnergyToDeposit == 0 &&
            StartPurchaseRapidHealingWindow(gameState, currentPlayer, card, command) is { } window)
        {
            return window;
        }

        var stepResult = _marketPurchaseService.BuyFaceUpCard(gameState, command.SlotIndex, effectiveCost, command.StoredEnergyToDeposit);
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
        var stepResult = _rapidHealingService.Activate(gameState, command.ActorPlayerId!.Value);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, gameState.PendingDecision);
    }

    private CommandResult ExecuteActivateHealingRay(GameState gameState, ActivateHealingRayCommand command)
    {
        var (healer, target, currentTurn) = EnsureCanActivateHealingRay(gameState, command);
        var healingResult = new HealingRayService().HealOtherPlayer(healer, target, command.HealingAmount);
        if (healingResult.HealedAmount <= 0)
        {
            throw new InvalidOperationException("Healing Ray did not heal any damage.");
        }

        currentTurn.SpendHealingRayHearts(healingResult.HealedAmount);

        var events = new GameEventBase[]
        {
            new PlayerHealedEvent(target.PlayerId, healingResult.HealedAmount, "Keep card: Healing Ray.")
        };

        PublishEvents(events);
        return CommandResult.Successful(gameState, events);
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
            new MimicTargetCleanupService().ClearTargetsForLostCard(gameState, actor.PlayerId, discardedCard.CardId);
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

    private CommandResult ExecuteBuyPeekedTopDeckCard(GameState gameState, BuyPeekedTopDeckCardCommand command, bool allowRapidHealingWindow = true)
    {
        var topCard = gameState.Market.PeekTopDrawCard();
        var currentPlayer = gameState.GetCurrentPlayer();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(currentPlayer, topCard);
        _validator.EnsureCanBuyPeekedTopDeckCard(gameState, command, effectiveCost);
        if (allowRapidHealingWindow && command.StoredEnergyToDeposit == 0 &&
            StartPurchaseRapidHealingWindow(gameState, currentPlayer, topCard, command) is { } window)
        {
            return window;
        }

        var stepResult = _marketPurchaseService.BuyTopDeckCard(gameState, effectiveCost, command.StoredEnergyToDeposit);
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

    private CommandResult ExecuteBuyOpportunistRevealedCard(GameState gameState, BuyOpportunistRevealedCardCommand command, bool allowRapidHealingWindow = true)
    {
        var payload = EnsureCanBuyOpportunistRevealedCard(gameState, command);
        var actor = gameState.GetPlayerById(command.ActorPlayerId!.Value);
        var card = gameState.Market.FaceUpCards[payload.SlotIndex]
            ?? throw new InvalidOperationException("Selected market slot is empty.");
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(actor, card);
        if (allowRapidHealingWindow && command.StoredEnergyToDeposit == 0 &&
            StartPurchaseRapidHealingWindow(gameState, actor, card, command) is { } window)
        {
            return window;
        }

        var stepResult = _marketPurchaseService.BuyOpportunistRevealedCard(
            gameState,
            actor.PlayerId,
            payload.SlotIndex,
            effectiveCost,
            command.StoredEnergyToDeposit);
        var nextDecision = gameState.ResolveOpportunistDecision();
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, nextDecision);
    }

    private CommandResult ExecuteDeclineOpportunistRevealedCard(GameState gameState, DeclineOpportunistRevealedCardCommand command)
    {
        EnsureCanDeclineOpportunistRevealedCard(gameState, command);
        var nextDecision = gameState.ResolveOpportunistDecision();
        return CommandResult.Successful(gameState, pendingDecision: nextDecision);
    }

    private CommandResult ExecuteEndTurn(GameState gameState, EndTurnCommand command)
    {
        _validator.EnsureCanEndTurn(gameState, command);
        var currentPlayer = gameState.GetCurrentPlayer();
        if (currentPlayer.Status.PoisonTokens >= currentPlayer.Health &&
            CanPreventLethalDamage(currentPlayer))
        {
            var turn = gameState.CurrentTurn!;
            turn.StartRapidHealingWindow(new[] { currentPlayer.PlayerId }, 0, endTurn: true);
            var decision = CreateRapidHealingDecision(currentPlayer.PlayerId);
            gameState.SetPendingDecision(decision);
            return CommandResult.Successful(gameState, pendingDecision: decision);
        }

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
            throw new InvalidOperationException("Cannot activate Healing Ray while another decision is pending.");
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

        if (!HasHealingRayEffect(healer))
        {
            throw new InvalidOperationException("Player cannot use Healing Ray right now.");
        }

        var unusedHeartCount = gameState.CurrentTurn.DicePool.Dice.Count(die => die.CurrentFace == DieFace.Heart) -
                               gameState.CurrentTurn.HealingRayHeartsSpent -
                               gameState.CurrentTurn.HeartsUsedElsewhere;
        if (command.HealingAmount > unusedHeartCount)
        {
            throw new InvalidOperationException("Not enough unused heart dice for Healing Ray.");
        }

        return (healer, gameState.GetPlayerById(command.TargetPlayerId), gameState.CurrentTurn);
    }

    private static bool HasHealingRayEffect(PlayerState player)
    {
        return player.KeepCards.Any(card =>
            card.CardId == KnownCardIds.HealingRay ||
            (card.CardId == KnownCardIds.Mimic && card.MimicTarget?.CardId == KnownCardIds.HealingRay));
    }

    private static (PlayerState Buyer, PlayerState Seller, MarketCardState Card) EnsureCanBuyOwnedKeepCard(GameState gameState, BuyOwnedKeepCardCommand command)
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
    }

    private static void EnsureCanActivatePsychicProbe(GameState gameState, ActivatePsychicProbeCommand command)
    {
        if (gameState.Status != GameStatus.Running)
        {
            throw new InvalidOperationException("Cannot activate Psychic Probe when game is not running.");
        }

        if (gameState.CurrentTurn is null)
        {
            throw new InvalidOperationException("Cannot activate Psychic Probe without an active turn.");
        }

        if (gameState.CurrentTurn.Phase != TurnPhase.Rolling || gameState.CurrentTurn.DiceResolved)
        {
            throw new InvalidOperationException("Psychic Probe can only be used during the rolling phase before dice are finalized.");
        }

        if (gameState.CurrentTurn.RollCountUsed <= 0)
        {
            throw new InvalidOperationException("Psychic Probe can only be used after at least one roll.");
        }

        if (gameState.PendingDecision is not null &&
            gameState.PendingDecision.DecisionType != DecisionType.SelectDiceToReroll)
        {
            throw new InvalidOperationException("Cannot activate Psychic Probe while another decision is pending.");
        }

        if (!command.ActorPlayerId.HasValue)
        {
            throw new InvalidOperationException("ActivatePsychicProbeCommand requires an actor player id.");
        }

        if (command.ActorPlayerId.Value == gameState.CurrentTurn.CurrentPlayerId)
        {
            throw new InvalidOperationException("Psychic Probe can only be used during another player's turn.");
        }

        var actor = gameState.GetPlayerById(command.ActorPlayerId.Value);
        if (!actor.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot activate Psychic Probe.");
        }

        var psychicProbe = actor.KeepCards.SingleOrDefault(card => card.CardId == KnownCardIds.PsychicProbe);
        if (psychicProbe is null)
        {
            throw new InvalidOperationException("Player cannot use Psychic Probe right now.");
        }

        if (psychicProbe.Counters > 0)
        {
            throw new InvalidOperationException("Psychic Probe can only be used once during each other player's turn.");
        }

        if (command.TargetDieIndex < 0 || command.TargetDieIndex >= gameState.CurrentTurn.DicePool.Dice.Count)
        {
            throw new InvalidOperationException("Selected die index is invalid.");
        }
    }

    private static void ResetPsychicProbeUsage(GameState gameState)
    {
        foreach (var card in gameState.Players.SelectMany(player => player.KeepCards)
                     .Where(card => card.CardId == KnownCardIds.PsychicProbe && card.Counters > 0))
        {
            card.ResetCounters();
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
                CurrentLockedDiceIndexes = currentTurn.DicePool.Dice.Where(d => d.IsLocked).Select(d => d.Index).ToArray(),
                CurrentFaces = currentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray(),
                RollCountUsed = currentTurn.RollCountUsed,
                MaxRolls = currentTurn.MaxRolls
            }
        };
    }

    private void PublishEvents(IEnumerable<GameEventBase> eventsToPublish)
    {
        foreach (var gameEvent in eventsToPublish)
        {
            foreach (var observer in _observers)
            {
                observer.OnEvent(gameEvent);
            }
        }
    }
}
