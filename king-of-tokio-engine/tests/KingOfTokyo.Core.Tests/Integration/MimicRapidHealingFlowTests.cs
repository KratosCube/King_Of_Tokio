using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicRapidHealingFlowTests
{
    [Fact]
    public void ActivateMimickedRapidHealing_Should_HealOneAndSpendTwoEnergy()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.RapidHealing, "Rapid Healing"));
        player.GainEnergy(RapidHealingService.ActivationCost);
        player.TakeDamage(3);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateMimickedRapidHealingCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, player.Energy);
        Assert.Equal(8, player.Health);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.RapidHealing));
        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                             healed.PlayerId == player.PlayerId &&
                                             healed.Amount == 1);
    }

    [Fact]
    public void ActivateMimickedRapidHealing_Should_Fail_WhenMimicTargetsDifferentCard()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.HerdCuller, "Herd Culler"));
        player.GainEnergy(RapidHealingService.ActivationCost);
        player.TakeDamage(3);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateMimickedRapidHealingCommand(player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use mimicked Rapid Healing right now.", result.Error);
        Assert.Equal(RapidHealingService.ActivationCost, player.Energy);
        Assert.Equal(7, player.Health);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateMimicCopying(string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(1, copiedCardId, copiedCardName));
    }
}
