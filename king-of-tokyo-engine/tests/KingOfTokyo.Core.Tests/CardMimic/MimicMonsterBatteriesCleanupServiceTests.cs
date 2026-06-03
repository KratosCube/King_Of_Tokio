using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardMimic;

public sealed class MimicMonsterBatteriesCleanupServiceTests
{
    [Fact]
    public void SpendEnergy_Should_ClearMimicTarget_WhenMonsterBatteriesAreDiscarded()
    {
        var gameState = CreateGameState(3);
        var batteryOwner = gameState.GetPlayerById(0);
        var mimicOwner = gameState.GetPlayerById(1);
        var battery = CreateKeepCard(KnownCardIds.MonsterBatteries, "Monster Batteries", 3, storedEnergy: 2);
        var mimic = CreateMimicCopying(batteryOwner.PlayerId, KnownCardIds.MonsterBatteries, "Monster Batteries");
        batteryOwner.AddKeepCard(battery);
        mimicOwner.AddKeepCard(mimic);
        var service = new EnergyPaymentService();

        var events = service.SpendEnergy(gameState, batteryOwner, 2, "Test payment.");

        Assert.Null(mimic.MimicTarget);
        Assert.False(batteryOwner.HasKeepCard(KnownCardIds.MonsterBatteries));
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.MonsterBatteries, gameState.Market.DiscardPile[0].CardId);
        Assert.Single(events);
    }

    [Fact]
    public void SpendEnergy_Should_NotClearMimicTarget_WhenMonsterBatteriesKeepStoredEnergy()
    {
        var gameState = CreateGameState(3);
        var batteryOwner = gameState.GetPlayerById(0);
        var mimicOwner = gameState.GetPlayerById(1);
        var battery = CreateKeepCard(KnownCardIds.MonsterBatteries, "Monster Batteries", 3, storedEnergy: 2);
        var mimic = CreateMimicCopying(batteryOwner.PlayerId, KnownCardIds.MonsterBatteries, "Monster Batteries");
        batteryOwner.AddKeepCard(battery);
        mimicOwner.AddKeepCard(mimic);
        var service = new EnergyPaymentService();

        var events = service.SpendEnergy(gameState, batteryOwner, 1, "Test payment.");

        Assert.NotNull(mimic.MimicTarget);
        Assert.True(batteryOwner.HasKeepCard(KnownCardIds.MonsterBatteries));
        Assert.Equal(1, battery.StoredEnergy);
        Assert.Empty(gameState.Market.DiscardPile);
        Assert.Empty(events);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost, int storedEnergy = 0)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep,
            storedEnergy: storedEnergy);
    }

    private static MarketCardState CreateMimicCopying(int ownerPlayerId, string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(ownerPlayerId, copiedCardId, copiedCardName));
    }
}
