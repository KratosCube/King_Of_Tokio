using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Events;
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
                FinalizeDiceCommand finalizeDiceCommand => ExecuteFinalizeDice(gameState, finalizeDiceCommand),
                ChooseLeaveTokyoCommand chooseLeaveTokyoCommand => ExecuteChooseLeaveTokyo(gameState, chooseLeaveTokyoCommand),
                BuyFaceUpCardCommand buyFaceUpCardCommand => ExecuteBuyFaceUpCard(gameState, buyFaceUpCardCommand),
                RefreshMarketCommand refreshMarketCommand => ExecuteRefreshMarket(gameState, refreshMarketCommand),
                ActivateRapidHealingCommand activateRapidHealingCommand => ExecuteActivateRapidHealing(gameState, activateRapidHealingCommand),
                ActivateTelepathCommand activateTelepathCommand => ExecuteActivateTelepath(gameState, activateTelepathCommand),
                ActivateStretchyCommand activateStretchyCommand => ExecuteActivateStretchy(gameState, activateStretchyCommand),
                ActivateHerdCullerCommand activateHerdCullerCommand => ExecuteActivateHerdCuller(gameState, activateHerdCullerCommand),
                ActivatePlotTwistCommand activatePlotTwistCommand => ExecuteActivatePlotTwist(gameState, activatePlotTwistCommand),
                ActivateMetamorphCommand activateMetamorphCommand => ExecuteActivateMetamorph(gameState, activateMetamorphCommand),
                PeekTopDeckCardCommand peekTopDeckCardCommand => ExecutePeekTopDeckCard(gameState, peekTopDeckCardCommand),
                BuyPeekedTopDeckCardCommand buyPeekedTopDeckCardCommand => ExecuteBuyPeekedTopDeckCard(gameState, buyPeekedTopDeckCardCommand),
                DeclinePeekedTopDeckCardCommand declinePeekedTopDeckCardCommand => ExecuteDeclinePeekedTopDeckCard(gameState, declinePeekedTopDeckCardCommand),
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
        var currentPlayer = gameState.GetCurrentPlayer();
        var diceCount = _keepCardRulesService.GetEffectiveDiceCount(currentPlayer);
        var maxRolls = 3 + _keepCardRulesService.GetExtraRerolls(currentPlayer);
        gameState.StartTurnForCurrentPlayer(diceCount: diceCount, maxRolls: maxRolls);
        currentPlayer = gameState.GetCurrentPlayer();
        var newEvents = new List<GameEventBase> { new TurnStartedEvent(currentPlayer.PlayerId) };

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
        _diceRollService.RollAll(currentTurn.DicePool);
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
        _diceRollService.RerollSelected(currentTurn.DicePool, command.DiceIndexesToReroll);
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
        var stepResult = _finalizeDiceService.Execute(gameState);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteChooseLeaveTokyo(GameState gameState, ChooseLeaveTokyoCommand command)
    {
        _validator.EnsureCanChooseLeaveTokyo(gameState, command);
        var stepResult = _tokyoDecisionService.Execute(gameState, command.LeaveTokyo);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
    }

    private CommandResult ExecuteBuyFaceUpCard(GameState gameState, BuyFaceUpCardCommand command)
    {
        var card = gameState.Market.FaceUpCards[command.SlotIndex] ?? throw new InvalidOperationException("Selected market slot is empty.");
        var currentPlayer = gameState.GetCurrentPlayer();
        var effectiveCost = _keepCardRulesService.GetEffectivePurchaseCost(currentPlayer, card);
        _validator.EnsureCanBuyFaceUpCard(gameState, command, effectiveCost);
        var stepResult = _marketPurchaseService.BuyFaceUpCard(gameState, command.SlotIndex, effectiveCost);
        PublishEvents(stepResult.Events);
        return CommandResult.Successful(gameState, stepResult.Events, stepResult.PendingDecision);
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

    private static PendingDecision? CreateRerollDecisionIfAvailable(KingOfTokyo.Core.Domain.Entities.TurnState currentTurn)
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