using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.State;
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

        return new EngineStepResult(events);
    }
}