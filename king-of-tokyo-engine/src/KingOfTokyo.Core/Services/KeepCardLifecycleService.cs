using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class KeepCardLifecycleService
{
    public void ApplyAddedEffect(PlayerState player, MarketCardState card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        if (card.CardId == KnownCardIds.EvenBigger)
        {
            player.IncreaseMaxHealth(2);
        }
    }

    public void ApplyLostEffect(PlayerState player, MarketCardState card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        if (card.CardId == KnownCardIds.EvenBigger)
        {
            player.DecreaseMaxHealth(2);
        }
    }
}
