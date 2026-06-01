using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class MarketRefreshService
{
    public const int RefreshCost = 2;

    private readonly EnergyPaymentService _energyPaymentService;

    public MarketRefreshService(EnergyPaymentService? energyPaymentService = null)
    {
        _energyPaymentService = energyPaymentService ?? new EnergyPaymentService();
    }

    public EngineStepResult Refresh(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot refresh market without an active turn.");

        var player = gameState.GetCurrentPlayer();

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            RefreshCost,
            "Keep card: Monster Batteries.");

        var refreshedCards = gameState.Market.RefreshAllFaceUpCards();
        currentTurn.Flags.BoughtCard = true;

        var events = new List<GameEventBase>(paymentEvents)
        {
            new MarketRefreshedEvent(
                player.PlayerId,
                RefreshCost,
                refreshedCards.Select(card => card.CardId).ToArray())
        };

        return new EngineStepResult(events);
    }
}