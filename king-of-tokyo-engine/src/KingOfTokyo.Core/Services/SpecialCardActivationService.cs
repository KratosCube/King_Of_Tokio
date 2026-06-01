using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Engine;

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