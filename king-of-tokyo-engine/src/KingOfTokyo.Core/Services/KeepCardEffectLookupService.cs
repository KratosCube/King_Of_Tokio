using KingOfTokyo.Core.Domain.Entities;
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
}
