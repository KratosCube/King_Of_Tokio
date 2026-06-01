using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class EnergyPaymentService
{
    public int GetAvailableEnergy(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.Energy + player.KeepCards
            .Where(card => card.CardId == KnownCardIds.MonsterBatteries)
            .Sum(card => card.StoredEnergy);
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

        var events = new List<GameEventBase>();
        var remainingAmount = amount;
        var playerEnergyToSpend = Math.Min(player.Energy, remainingAmount);

        if (playerEnergyToSpend > 0)
        {
            player.SpendEnergy(playerEnergyToSpend);
            remainingAmount -= playerEnergyToSpend;
        }

        if (remainingAmount == 0)
        {
            return events;
        }

        foreach (var battery in player.KeepCards
                     .Where(card => card.CardId == KnownCardIds.MonsterBatteries && card.StoredEnergy > 0)
                     .ToArray())
        {
            var storedEnergyToSpend = Math.Min(battery.StoredEnergy, remainingAmount);
            battery.SpendStoredEnergy(storedEnergyToSpend);
            remainingAmount -= storedEnergyToSpend;

            if (battery.StoredEnergy == 0)
            {
                var discardedCard = player.RemoveKeepCard(KnownCardIds.MonsterBatteries);
                gameState.Market.Discard(discardedCard);

                events.Add(new KeepCardDiscardedEvent(
                    player.PlayerId,
                    discardedCard.CardId,
                    discardedCard.Name,
                    discardReason));
            }

            if (remainingAmount == 0)
            {
                break;
            }
        }

        return events;
    }
}
