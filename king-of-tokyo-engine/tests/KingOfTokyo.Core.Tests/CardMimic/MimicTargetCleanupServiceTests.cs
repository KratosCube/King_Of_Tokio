using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardMimic;

public sealed class MimicTargetCleanupServiceTests
{
    [Fact]
    public void ClearTargetsForLostCard_Should_ClearMatchingMimicTargets()
    {
        var gameState = CreateGameState();
        var firstMimicOwner = gameState.GetPlayerById(0);
        var originalOwner = gameState.GetPlayerById(1);
        var secondMimicOwner = gameState.GetPlayerById(2);
        var firstMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        var secondMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        firstMimicOwner.AddKeepCard(firstMimic);
        secondMimicOwner.AddKeepCard(secondMimic);
        var service = new MimicTargetCleanupService();

        var clearedCount = service.ClearTargetsForLostCard(gameState, originalOwner.PlayerId, KnownCardIds.HealingRay);

        Assert.Equal(2, clearedCount);
        Assert.Null(firstMimic.MimicTarget);
        Assert.Null(secondMimic.MimicTarget);
    }

    [Fact]
    public void ClearTargetsForLostCard_Should_NotClearDifferentCardOrOwnerTargets()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var originalOwner = gameState.GetPlayerById(1);
        var otherOwner = gameState.GetPlayerById(2);
        var differentCardMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.RapidHealing, "Rapid Healing");
        var differentOwnerMimic = CreateMimicCopying(otherOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        mimicOwner.AddKeepCard(differentCardMimic);
        mimicOwner.AddKeepCard(differentOwnerMimic);
        var service = new MimicTargetCleanupService();

        var clearedCount = service.ClearTargetsForLostCard(gameState, originalOwner.PlayerId, KnownCardIds.HealingRay);

        Assert.Equal(0, clearedCount);
        Assert.NotNull(differentCardMimic.MimicTarget);
        Assert.Equal(KnownCardIds.RapidHealing, differentCardMimic.MimicTarget!.CardId);
        Assert.NotNull(differentOwnerMimic.MimicTarget);
        Assert.Equal(otherOwner.PlayerId, differentOwnerMimic.MimicTarget!.OwnerPlayerId);
    }

    [Fact]
    public void ClearTargetsForLostCard_Should_IgnoreUntargetedMimic()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var originalOwner = gameState.GetPlayerById(1);
        var untargetedMimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        mimicOwner.AddKeepCard(untargetedMimic);
        var service = new MimicTargetCleanupService();

        var clearedCount = service.ClearTargetsForLostCard(gameState, originalOwner.PlayerId, KnownCardIds.HealingRay);

        Assert.Equal(0, clearedCount);
        Assert.Null(untargetedMimic.MimicTarget);
    }

    [Fact]
    public void ClearTargetsForOwner_Should_ClearAllTargetsPointingToOwner()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var originalOwner = gameState.GetPlayerById(1);
        var healingRayMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        var rapidHealingMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.RapidHealing, "Rapid Healing");
        mimicOwner.AddKeepCard(healingRayMimic);
        mimicOwner.AddKeepCard(rapidHealingMimic);
        var service = new MimicTargetCleanupService();

        var clearedCount = service.ClearTargetsForOwner(gameState, originalOwner.PlayerId);

        Assert.Equal(2, clearedCount);
        Assert.Null(healingRayMimic.MimicTarget);
        Assert.Null(rapidHealingMimic.MimicTarget);
    }

    [Fact]
    public void ClearTargetsForOwner_Should_NotClearTargetsPointingToDifferentOwner()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var originalOwner = gameState.GetPlayerById(1);
        var otherOwner = gameState.GetPlayerById(2);
        var matchingMimic = CreateMimicCopying(originalOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        var differentOwnerMimic = CreateMimicCopying(otherOwner.PlayerId, KnownCardIds.RapidHealing, "Rapid Healing");
        mimicOwner.AddKeepCard(matchingMimic);
        mimicOwner.AddKeepCard(differentOwnerMimic);
        var service = new MimicTargetCleanupService();

        var clearedCount = service.ClearTargetsForOwner(gameState, originalOwner.PlayerId);

        Assert.Equal(1, clearedCount);
        Assert.Null(matchingMimic.MimicTarget);
        Assert.NotNull(differentOwnerMimic.MimicTarget);
        Assert.Equal(otherOwner.PlayerId, differentOwnerMimic.MimicTarget!.OwnerPlayerId);
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

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
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
