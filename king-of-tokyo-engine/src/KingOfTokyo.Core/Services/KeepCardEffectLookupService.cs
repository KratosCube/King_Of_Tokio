using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class KeepCardEffectLookupService
{
    public bool HasEffect(PlayerState player, string cardId)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        return CountEffects(player, cardId) > 0;
    }

    public bool HasEffect(GameState gameState, PlayerState player, string cardId)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        return CountEffects(gameState, player, cardId) > 0;
    }

    public int CountEffects(PlayerState player, string cardId)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        return player.KeepCards.Count(card =>
            card.CardId == cardId ||
            (card.CardId == KnownCardIds.Mimic && card.MimicTarget?.CardId == cardId));
    }

    public int CountEffects(GameState gameState, PlayerState player, string cardId)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        return player.KeepCards.Count(card =>
            card.CardId == cardId || IsValidMimicTarget(gameState, card, cardId));
    }

    private static bool IsValidMimicTarget(GameState gameState, MarketCardState card, string cardId)
    {
        if (card.CardId != KnownCardIds.Mimic || card.MimicTarget?.CardId != cardId)
        {
            return false;
        }

        var targetOwner = gameState.GetPlayerById(card.MimicTarget.OwnerPlayerId);
        return targetOwner.IsAlive && targetOwner.KeepCards.Any(targetCard => targetCard.CardId == cardId);
    }
}
