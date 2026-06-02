using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class MarketRefreshService
{
    public const int RefreshCost = 2;

    public EngineStepResult Refresh(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot refresh market without an active turn.");

        var player = gameState.GetCurrentPlayer();

        player.SpendEnergy(RefreshCost);

        var refreshedCards = gameState.Market.RefreshAllFaceUpCards();
        currentTurn.Flags.BoughtCard = true;

        var events = new GameEventBase[]
        {
            new MarketRefreshedEvent(
                player.PlayerId,
                RefreshCost,
                refreshedCards.Select(card => card.CardId).ToArray())
        };

        var pendingDecision = CreateOpportunistDecisionForFirstRevealedCard(gameState);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(events, pendingDecision);
    }

    private static PendingDecision? CreateOpportunistDecisionForFirstRevealedCard(GameState gameState)
    {
        for (var slotIndex = 0; slotIndex < gameState.Market.FaceUpCards.Count; slotIndex++)
        {
            var revealedCard = gameState.Market.FaceUpCards[slotIndex];
            if (revealedCard is null)
            {
                continue;
            }

            var eligiblePlayerIds = gameState.Players
                .Where(player => player.IsAlive &&
                                 player.HasKeepCard(KnownCardIds.Opportunist) &&
                                 player.Energy >= revealedCard.Cost)
                .Select(player => player.PlayerId)
                .ToArray();

            if (eligiblePlayerIds.Length == 0)
            {
                continue;
            }

            return new PendingDecision
            {
                DecisionType = DecisionType.OpportunistPurchase,
                PlayerId = eligiblePlayerIds[0],
                Payload = new MarketCardRevealDecisionData
                {
                    SlotIndex = slotIndex,
                    CardId = revealedCard.CardId,
                    CardName = revealedCard.Name,
                    Cost = revealedCard.Cost,
                    EligiblePlayerIds = eligiblePlayerIds
                }
            };
        }

        return null;
    }
}
