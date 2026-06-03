using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Services;

public sealed class MimicServiceTests
{
    [Fact]
    public void SetTarget_Should_SetMimicCopiedCardTarget()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var target = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        mimicOwner.AddKeepCard(mimic);
        targetOwner.AddKeepCard(target);
        var service = new MimicService();

        service.SetTarget(gameState, mimicOwner.PlayerId, targetOwner.PlayerId, target.CardId);

        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(targetOwner.PlayerId, mimic.MimicTarget!.OwnerPlayerId);
        Assert.Equal(target.CardId, mimic.MimicTarget.CardId);
        Assert.Equal(target.Name, mimic.MimicTarget.CardName);
    }

    [Fact]
    public void SetTarget_Should_RetargetExistingMimicTarget()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var firstTarget = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        var secondTarget = CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail", 5);
        mimicOwner.AddKeepCard(mimic);
        targetOwner.AddKeepCard(firstTarget);
        targetOwner.AddKeepCard(secondTarget);
        var service = new MimicService();

        service.SetTarget(gameState, mimicOwner.PlayerId, targetOwner.PlayerId, firstTarget.CardId);
        service.SetTarget(gameState, mimicOwner.PlayerId, targetOwner.PlayerId, secondTarget.CardId);

        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(targetOwner.PlayerId, mimic.MimicTarget!.OwnerPlayerId);
        Assert.Equal(secondTarget.CardId, mimic.MimicTarget.CardId);
        Assert.Equal(secondTarget.Name, mimic.MimicTarget.CardName);
    }

    [Fact]
    public void SetTarget_Should_Throw_WhenPlayerDoesNotHaveMimic()
    {
        var gameState = CreateGameState();
        var targetOwner = gameState.GetPlayerById(1);
        var target = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        targetOwner.AddKeepCard(target);
        var service = new MimicService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.SetTarget(gameState, 0, targetOwner.PlayerId, target.CardId));

        Assert.Equal("Player does not have Mimic.", exception.Message);
    }

    [Fact]
    public void SetTarget_Should_Throw_WhenTargetIsMimic()
    {
        var gameState = CreateGameState();
        var mimicOwner = gameState.GetPlayerById(0);
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var targetMimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        mimicOwner.AddKeepCard(mimic);
        targetOwner.AddKeepCard(targetMimic);
        var service = new MimicService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.SetTarget(gameState, mimicOwner.PlayerId, targetOwner.PlayerId, targetMimic.CardId));

        Assert.Equal("Mimic cannot copy another Mimic.", exception.Message);
        Assert.Null(mimic.MimicTarget);
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
}
