using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class MimicService
{
    public void SetTarget(GameState gameState, int mimicOwnerPlayerId, int targetOwnerPlayerId, string targetCardId)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (string.IsNullOrWhiteSpace(targetCardId))
        {
            throw new ArgumentException("Target card id must not be empty.", nameof(targetCardId));
        }

        var mimicOwner = gameState.GetPlayerById(mimicOwnerPlayerId);
        if (!mimicOwner.IsAlive)
        {
            throw new InvalidOperationException("Dead players cannot use Mimic.");
        }

        var mimicCard = mimicOwner.KeepCards.FirstOrDefault(card => card.CardId == KnownCardIds.Mimic)
            ?? throw new InvalidOperationException("Player does not have Mimic.");

        var targetOwner = gameState.GetPlayerById(targetOwnerPlayerId);
        if (!targetOwner.IsAlive)
        {
            throw new InvalidOperationException("Mimic cannot copy a card owned by a dead player.");
        }

        if (mimicOwner.PlayerId == targetOwner.PlayerId)
        {
            throw new InvalidOperationException("Mimic cannot copy its owner's own cards.");
        }

        var targetCard = targetOwner.KeepCards.FirstOrDefault(card => card.CardId == targetCardId)
            ?? throw new InvalidOperationException("Target player does not own the selected keep card.");

        if (targetCard.CardType != MarketCardType.Keep)
        {
            throw new InvalidOperationException("Mimic can only copy keep cards.");
        }

        if (targetCard.CardId == KnownCardIds.Mimic)
        {
            throw new InvalidOperationException("Mimic cannot copy another Mimic.");
        }

        mimicCard.SetMimicTarget(new MimicTargetState(
            targetOwner.PlayerId,
            targetCard.CardId,
            targetCard.Name));
    }
}
