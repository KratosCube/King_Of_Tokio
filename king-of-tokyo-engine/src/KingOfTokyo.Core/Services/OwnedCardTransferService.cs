using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Services;

public sealed class OwnedCardTransferService
{
    private readonly KeepCardLifecycleService _keepCardLifecycleService;

    public OwnedCardTransferService(KeepCardLifecycleService? keepCardLifecycleService = null)
    {
        _keepCardLifecycleService = keepCardLifecycleService ?? new KeepCardLifecycleService();
    }

    public void BuyKeepCardFromPlayer(GameState gameState, PlayerState buyer, PlayerState seller, string cardId, int cost)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var transferredCard = BuyKeepCardFromPlayer(buyer, seller, cardId, cost);

        _keepCardLifecycleService.ApplyLostEffect(seller, transferredCard);
        _keepCardLifecycleService.ApplyAddedEffect(buyer, transferredCard);

        var mimicTargetCleanupService = new MimicTargetCleanupService();
        mimicTargetCleanupService.ClearTargetsForLostCard(gameState, seller.PlayerId, cardId);
    }

    public MarketCardState BuyKeepCardFromPlayer(PlayerState buyer, PlayerState seller, string cardId, int cost)
    {
        ArgumentNullException.ThrowIfNull(buyer);
        ArgumentNullException.ThrowIfNull(seller);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost));
        }

        if (buyer.PlayerId == seller.PlayerId)
        {
            throw new InvalidOperationException("A player cannot buy a card from themselves.");
        }

        if (!buyer.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot buy cards.");
        }

        if (!seller.IsAlive)
        {
            throw new InvalidOperationException("Cannot buy cards from dead players.");
        }

        if (buyer.Energy < cost)
        {
            throw new InvalidOperationException("Buyer does not have enough energy.");
        }

        var transferredCard = seller.RemoveKeepCard(cardId);
        buyer.SpendEnergy(cost);
        seller.GainEnergy(cost);
        buyer.AddKeepCard(transferredCard);

        return transferredCard;
    }
}
