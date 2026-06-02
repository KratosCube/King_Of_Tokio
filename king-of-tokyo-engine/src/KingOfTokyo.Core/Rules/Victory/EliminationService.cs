using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Victory;

public sealed class EliminationService
{
    public bool TryEliminate(GameState gameState, PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (player.IsAlive)
        {
            return false;
        }

        RemovePlayerFromTokyoIfNeeded(gameState, player);
        DisableBayIfNeeded(gameState);

        if (player.HasKeepCard(KnownCardIds.ItHasAChild))
        {
            ApplyItHasAChild(gameState, player);
        }

        return true;
    }

    private static void ApplyItHasAChild(GameState gameState, PlayerState player)
    {
        foreach (var card in player.KeepCards.ToArray())
        {
            var removedCard = player.RemoveKeepCard(card.CardId);
            gameState.Market.Discard(removedCard);
        }

        if (player.Energy > 0)
        {
            player.SpendEnergy(player.Energy);
        }

        player.Heal(10);
    }

    private static void RemovePlayerFromTokyoIfNeeded(GameState gameState, PlayerState player)
    {
        if (player.TokyoSlot == TokyoSlot.City)
        {
            gameState.Tokyo.SetCityOccupant(null);
            player.SetTokyoSlot(TokyoSlot.None);
        }
        else if (player.TokyoSlot == TokyoSlot.Bay)
        {
            gameState.Tokyo.SetBayOccupant(null);
            player.SetTokyoSlot(TokyoSlot.None);
        }
    }

    private static void DisableBayIfNeeded(GameState gameState)
    {
        if (!gameState.Tokyo.BayEnabled)
        {
            return;
        }

        if (gameState.GetAlivePlayers().Count >= 5)
        {
            return;
        }

        if (gameState.Tokyo.BayOccupantId is int bayOccupantId)
        {
            var bayPlayer = gameState.GetPlayerById(bayOccupantId);
            bayPlayer.SetTokyoSlot(TokyoSlot.None);
            gameState.Tokyo.SetBayOccupant(null);
        }

        gameState.Tokyo.DisableBay();
    }
}
