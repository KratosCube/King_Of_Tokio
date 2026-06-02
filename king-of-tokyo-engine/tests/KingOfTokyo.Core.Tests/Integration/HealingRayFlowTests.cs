using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class HealingRayFlowTests
{
    [Fact]
    public void ActivateHealingRay_Should_HealTargetPayHealerAndSpendHeartDice()
    {
        var gameState = CreateGameState(3);
        var healer = gameState.GetCurrentPlayer();
        var target = gameState.GetPlayerById(1);
        healer.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray", 4));
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
        Assert.Equal(2, gameState.CurrentTurn.HealingRayHeartsSpent);
        Assert.Contains(result.NewEvents, e => e is KingOfTokyo.Core.Events.PlayerHealedEvent healed &&
                                              healed.PlayerId == target.PlayerId &&
                                              healed.Amount == 2);
    }

    [Fact]
    public void ActivateHealingRay_Should_Fail_WhenTryingToUseMoreHeartsThanRolled()
    {
        var gameState = CreateGameState(3);
        var healer = gameState.GetCurrentPlayer();
        var target = gameState.GetPlayerById(1);
        healer.AddKeepCard(CreateKeepCard(KnownCardIds.HealingRay, "Healing Ray", 4));
        target.TakeDamage(2);
        target.GainEnergy(4);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(healer.PlayerId));
        gameState.CurrentTurn!.DicePool.SetFace(0, DieFace.Heart);
        gameState.CurrentTurn.DicePool.SetFace(1, DieFace.Attack);
        gameState.CurrentTurn.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateHealingRayCommand(target.PlayerId, healingAmount: 2, healer.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Not enough unused heart dice for Healing Ray.", result.Error);
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
}
