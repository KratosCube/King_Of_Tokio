using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicTargetFlowTests
{
    [Fact]
    public void SetMimicTarget_Should_SetInitialTargetDuringPurchaseWithoutSpendingEnergy()
    {
        var gameState = CreateGameState(3);
        var mimicOwner = gameState.GetCurrentPlayer();
        var targetOwner = gameState.GetPlayerById(1);
        var mimic = CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8);
        var target = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        mimicOwner.AddKeepCard(mimic);
        mimicOwner.GainEnergy(1);
        targetOwner.AddKeepCard(target);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(mimicOwner.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, target.CardId, mimicOwner.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(1, mimicOwner.Energy);
        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(targetOwner.PlayerId, mimic.MimicTarget!.OwnerPlayerId);
        Assert.Equal(target.CardId, mimic.MimicTarget.CardId);
        Assert.Equal(target.Name, mimic.MimicTarget.CardName);
    }

    [Fact]
    public void SetMimicTarget_Should_RetargetAtStartOfTurnAndSpendOneEnergy()
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
        engine.Execute(gameState, new EndTurnCommand(mimicOwner.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Finished);
        gameState.CurrentPlayerIndex.GetType();

        gameState.AdvanceToNextAlivePlayer();
        gameState.AdvanceToNextAlivePlayer();
        gameState.AdvanceToNextAlivePlayer();
        engine.Execute(gameState, new BeginTurnCommand(mimicOwner.PlayerId));

        var result = engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, secondTarget.CardId, mimicOwner.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(1, mimicOwner.Energy);
        Assert.NotNull(mimic.MimicTarget);
        Assert.Equal(secondTarget.CardId, mimic.MimicTarget!.CardId);
        Assert.Equal(secondTarget.Name, mimic.MimicTarget.CardName);
    }

    [Fact]
    public void SetMimicTarget_Should_FailRetargetAfterRollingStarted()
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
        gameState.CurrentTurn.SetPhase(TurnPhase.Rolling);
        gameState.CurrentTurn.IncrementRollCount();

        var result = engine.Execute(gameState, new SetMimicTargetCommand(targetOwner.PlayerId, secondTarget.CardId, mimicOwner.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Mimic target can only be changed at the start of the owner's turn before rolling dice.", result.Error);
        Assert.Equal(2, mimicOwner.Energy);
        Assert.Equal(firstTarget.CardId, mimic.MimicTarget!.CardId);
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
