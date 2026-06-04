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

public sealed class MimicHealingRayFlowTests
{
    [Fact]
    public void ActivateHealingRay_Should_Work_WhenPlayerMimicsHealingRay()
    {
        var gameState = CreateGameState(3);
        var healer = gameState.GetCurrentPlayer();
        var copiedCardOwner = gameState.GetPlayerById(1);
        var target = gameState.GetPlayerById(2);
        copiedCardOwner.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray", 4));
        healer.AddKeepCard(CreateMimicCopying(copiedCardOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray"));
        target.TakeDamage(2);
        target.GainEnergy(4);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(healer.PlayerId));
        gameState.CurrentTurn!.DicePool.SetFace(0, DieFace.Heart);
        gameState.CurrentTurn.DicePool.SetFace(1, DieFace.Heart);
        gameState.CurrentTurn.DicePool.SetFace(2, DieFace.Attack);
        gameState.CurrentTurn.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateHealingRayCommand(target.PlayerId, healingAmount: 2, healer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(10, target.Health);
        Assert.Equal(0, target.Energy);
        Assert.Equal(4, healer.Energy);
        Assert.True(healer.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(healer.HasKeepCard(KnownCardIds.HealingRay));
        Assert.Equal(2, gameState.CurrentTurn.HealingRayHeartsSpent);
        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                             healed.PlayerId == target.PlayerId &&
                                             healed.Amount == 2);
    }

    [Fact]
    public void ActivateHealingRay_Should_Fail_WhenMimicTargetsDifferentCard()
    {
        var gameState = CreateGameState(3);
        var healer = gameState.GetCurrentPlayer();
        var target = gameState.GetPlayerById(1);
        healer.AddKeepCard(CreateMimicCopying(2, KnownCardIds.RapidHealing, "Rapid Healing"));
        target.TakeDamage(2);
        target.GainEnergy(4);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(healer.PlayerId));
        gameState.CurrentTurn!.DicePool.SetFace(0, DieFace.Heart);
        gameState.CurrentTurn.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateHealingRayCommand(target.PlayerId, healingAmount: 1, healer.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use Healing Ray right now.", result.Error);
        Assert.Equal(8, target.Health);
        Assert.Equal(4, target.Energy);
        Assert.Equal(0, healer.Energy);
        Assert.Equal(0, gameState.CurrentTurn.HealingRayHeartsSpent);
    }

    [Fact]
    public void ActivateHealingRay_Should_Fail_WhenCopiedCardOwnerLostHealingRay()
    {
        var gameState = CreateGameState(3);
        var healer = gameState.GetCurrentPlayer();
        var copiedCardOwner = gameState.GetPlayerById(1);
        var target = gameState.GetPlayerById(2);
        copiedCardOwner.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray", 4));
        healer.AddKeepCard(CreateMimicCopying(copiedCardOwner.PlayerId, KnownCardIds.HealingRay, "Healing Ray"));
        var removedCard = copiedCardOwner.RemoveKeepCard(KnownCardIds.HealingRay);
        new MimicTargetCleanupService().ClearTargetsForLostCard(gameState, copiedCardOwner.PlayerId, removedCard.CardId);
        target.TakeDamage(2);
        target.GainEnergy(4);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(healer.PlayerId));
        gameState.CurrentTurn!.DicePool.SetFace(0, DieFace.Heart);
        gameState.CurrentTurn.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateHealingRayCommand(target.PlayerId, healingAmount: 1, healer.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use Healing Ray right now.", result.Error);
        Assert.Equal(8, target.Health);
        Assert.Equal(4, target.Energy);
        Assert.Equal(0, healer.Energy);
        Assert.Equal(0, gameState.CurrentTurn.HealingRayHeartsSpent);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
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
