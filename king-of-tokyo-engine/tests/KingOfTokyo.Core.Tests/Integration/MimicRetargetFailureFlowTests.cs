using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicRetargetFailureFlowTests
{
    [Fact]
    public void SetMimicTarget_Should_FailRetargetToMimicWithoutSpendingEnergyOrChangingTarget()
    {
        var gameState = CreateGameState(3);
        var mimicOwner = gameState.GetCurrentPlayer();
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var firstTarget = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        var targetMimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        mimicOwner.AddKeepCard(mimic);
        mimicOwner.GainEnergy(2);
        targetOwner.AddKeepCard(firstTarget);
        targetOwner.AddKeepCard(targetMimic);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(mimicOwner.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);
        engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, firstTarget.CardId, mimicOwner.PlayerId));
        MoveBackToMimicOwnerTurn(gameState, engine, mimicOwner.PlayerId);

        var result = engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, targetMimic.CardId, mimicOwner.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Mimic cannot copy another Mimic.", result.Error);
        Assert.Equal(2, mimicOwner.Energy);
        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(firstTarget.CardId, mimic.MimicTarget!.CardId);
        Assert.Equal(firstTarget.Name, mimic.MimicTarget.CardName);
    }

    [Fact]
    public void SetMimicTarget_Should_FailRetargetToDeadOwnerWithoutSpendingEnergyOrChangingTarget()
    {
        var gameState = CreateGameState(3);
        var mimicOwner = gameState.GetCurrentPlayer();
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var firstTarget = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        var secondTarget = CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail", 5);
        mimicOwner.AddKeepCard(mimic);
        mimicOwner.GainEnergy(2);
        targetOwner.AddKeepCard(firstTarget);
        targetOwner.AddKeepCard(secondTarget);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(mimicOwner.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);
        engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, firstTarget.CardId, mimicOwner.PlayerId));
        MoveBackToMimicOwnerTurn(gameState, engine, mimicOwner.PlayerId);
        targetOwner.TakeDamage(10);

        var result = engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, secondTarget.CardId, mimicOwner.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Mimic cannot copy a card owned by a dead player.", result.Error);
        Assert.Equal(2, mimicOwner.Energy);
        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(firstTarget.CardId, mimic.MimicTarget!.CardId);
        Assert.Equal(firstTarget.Name, mimic.MimicTarget.CardName);
    }

    private static void MoveBackToMimicOwnerTurn(GameState gameState, GameEngine engine, int mimicOwnerPlayerId)
    {
        engine.Execute(gameState, new EndTurnCommand(mimicOwnerPlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Finished);

        gameState.AdvanceToNextAlivePlayer();
        gameState.AdvanceToNextAlivePlayer();
        gameState.AdvanceToNextAlivePlayer();
        engine.Execute(gameState, new BeginTurnCommand(mimicOwnerPlayerId));
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
