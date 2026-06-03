using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Victory;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicEliminationCleanupFlowTests
{
    [Fact]
    public void TryEliminate_Should_ClearMimicTargetsPointingToEliminatedPlayer()
    {
        var gameState = CreateGameState(4);
        var mimicOwner = gameState.GetPlayerById(0);
        var eliminatedPlayer = gameState.GetPlayerById(1);
        var otherTargetOwner = gameState.GetPlayerById(2);
        var matchingMimic = CreateMimicCopying(eliminatedPlayer.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        var otherOwnerMimic = CreateMimicCopying(otherTargetOwner.PlayerId, KnownCardIds.RapidHealing, "Rapid Healing");
        mimicOwner.AddKeepCard(matchingMimic);
        mimicOwner.AddKeepCard(otherOwnerMimic);
        eliminatedPlayer.TakeDamage(10);
        var service = new EliminationService();

        var eliminated = service.TryEliminate(gameState, eliminatedPlayer);

        Assert.True(eliminated);
        Assert.Null(matchingMimic.MimicTarget);
        Assert.NotNull(otherOwnerMimic.MimicTarget);
        Assert.Equal(otherTargetOwner.PlayerId, otherOwnerMimic.MimicTarget!.OwnerPlayerId);
    }

    [Fact]
    public void TryEliminate_Should_NotClearMimicTargets_WhenPlayerSurvives()
    {
        var gameState = CreateGameState(4);
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateMimicCopying(targetOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray");
        mimicOwner.AddKeepCard(mimic);
        targetOwner.TakeDamage(9);
        var service = new EliminationService();

        var eliminated = service.TryEliminate(gameState, targetOwner);

        Assert.False(eliminated);
        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(targetOwner.PlayerId, mimic.MimicTarget!.OwnerPlayerId);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
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
