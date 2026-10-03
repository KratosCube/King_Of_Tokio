using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class EnergyPaymentService
{
    public int GetAvailableEnergy(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.Energy;
    }

    public IReadOnlyList<GameEventBase> SpendEnergy(
        GameState gameState,
        PlayerState player,
        int amount,
        string discardReason)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(player);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(discardReason))
        {
            throw new ArgumentException("Discard reason must not be empty.", nameof(discardReason));
        }

        if (GetAvailableEnergy(player) < amount)
        {
            throw new InvalidOperationException("Cannot spend more energy than the player has available.");
        }

        player.SpendEnergy(amount);
        return Array.Empty<GameEventBase>();
    }
}
