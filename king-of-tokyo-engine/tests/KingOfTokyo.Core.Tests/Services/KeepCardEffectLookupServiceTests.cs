using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardMimic;

public sealed class KeepCardEffectLookupServiceTests
{
    [Fact]
    public void HasEffect_Should_ReturnTrue_WhenPlayerOwnsCard()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray"));
        var service = new KeepCardEffectLookupService();

        var hasEffect = service.HasEffect(player, KnownCardIds.HealingRay);

        Assert.True(hasEffect);
    }

    [Fact]
    public void HasEffect_Should_ReturnTrue_WhenMimicCopiesCard()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.HealingRay, "Healing Ray"));
        var service = new KeepCardEffectLookupService();

        var hasEffect = service.HasEffect(player, KnownCardIds.HealingRay);

        Assert.True(hasEffect);
    }

    [Fact]
    public void HasEffect_WithGameState_Should_ReturnTrue_WhenMimicTargetOwnerStillOwnsCard()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        targetOwner.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray"));
        mimicOwner.AddKeepCard(CreateMimicCopying(targetOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray"));
        var service = new KeepCardEffectLookupService();

        var hasEffect = service.HasEffect(gameState, mimicOwner, KnownCardIds.HealingRay);

        Assert.True(hasEffect);
    }

    [Fact]
    public void HasEffect_WithGameState_Should_ReturnFalse_WhenMimicTargetOwnerLostCard()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        targetOwner.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray"));
        mimicOwner.AddKeepCard(CreateMimicCopying(targetOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray"));
        targetOwner.RemoveKeepCard(KnownCardIds.HealingRay);
        var service = new KeepCardEffectLookupService();

        var hasEffect = service.HasEffect(gameState, mimicOwner, KnownCardIds.HealingRay);

        Assert.False(hasEffect);
    }

    [Fact]
    public void CountEffects_Should_CountOwnedCardAndMimicSeparately()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail"));
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.SpikedTail, "Spiked Tail"));
        var service = new KeepCardEffectLookupService();

        var effectCount = service.CountEffects(player, KnownCardIds.SpikedTail);

        Assert.Equal(2, effectCount);
    }

    [Fact]
    public void HasEffect_Should_ReturnFalse_WhenMimicTargetsDifferentCard()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.RapidHealing, "Rapid Healing"));
        var service = new KeepCardEffectLookupService();

        var hasEffect = service.HasEffect(player, KnownCardIds.HealingRay);

        Assert.False(hasEffect);
    }

    [Fact]
    public void CountEffects_Should_Throw_WhenCardIdIsEmpty()
    {
        var player = new PlayerState(0, "Monster");
        var service = new KeepCardEffectLookupService();

        Assert.Throws<ArgumentException>(() => service.CountEffects(player, ""));
    }

    private static GameState CreateGameState()
    {
        var players = new[]
        {
            new PlayerState(0, "Monster 1"),
            new PlayerState(1, "Monster 2"),
            new PlayerState(2, "Monster 3")
        };

        return new GameState(players, new GameOptions(players.Length));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            4,
            MarketCardType.Keep);
    }

    private static MarketCardState CreateMimicCopying(string copiedCardId, string copiedCardName)
    {
        return CreateMimicCopying(1, copiedCardId, copiedCardName);
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
