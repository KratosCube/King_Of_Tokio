using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Rules.Tokyo;

public sealed class TokyoResolver
{
    public bool IsTokyoCompletelyEmpty(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        return gameState.Tokyo.CityOccupantId is null && gameState.Tokyo.BayOccupantId is null;
    }

    public TokyoSlot? GetPreferredAvailableSlot(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (gameState.Tokyo.CityOccupantId is null)
        {
            return TokyoSlot.City;
        }

        if (gameState.Tokyo.BayEnabled && gameState.Tokyo.BayOccupantId is null)
        {
            return TokyoSlot.Bay;
        }

        return null;
    }

    public TokyoSlot EnterTokyo(GameState gameState, PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsAlive)
        {
            throw new InvalidOperationException("Dead player cannot enter Tokyo.");
        }

        if (player.TokyoSlot != TokyoSlot.None)
        {
            throw new InvalidOperationException("Player is already in Tokyo.");
        }

        var slot = GetPreferredAvailableSlot(gameState);
        if (slot is null)
        {
            throw new InvalidOperationException("No Tokyo slot is available.");
        }

        if (slot == TokyoSlot.City)
        {
            gameState.Tokyo.SetCityOccupant(player.PlayerId);
            player.SetTokyoSlot(TokyoSlot.City);
            return TokyoSlot.City;
        }

        gameState.Tokyo.SetBayOccupant(player.PlayerId);
        player.SetTokyoSlot(TokyoSlot.Bay);
        return TokyoSlot.Bay;
    }

    public void LeaveTokyo(GameState gameState, PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (player.TokyoSlot == TokyoSlot.City)
        {
            gameState.Tokyo.SetCityOccupant(null);
            player.SetTokyoSlot(TokyoSlot.None);
            return;
        }

        if (player.TokyoSlot == TokyoSlot.Bay)
        {
            gameState.Tokyo.SetBayOccupant(null);
            player.SetTokyoSlot(TokyoSlot.None);
            return;
        }

        throw new InvalidOperationException("Player is not in Tokyo.");
    }
}