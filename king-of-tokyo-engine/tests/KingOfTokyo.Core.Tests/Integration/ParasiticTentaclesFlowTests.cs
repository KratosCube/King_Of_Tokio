using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class ParasiticTentaclesFlowTests
{
    [Fact]
    public void BuyOwnedKeepCard_Should_TransferKeepCardAndPaySeller_WhenCurrentPlayerHasParasiticTentacles()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var seller = gameState.GetPlayerById(1);
        var transferredCard = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);

        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.ParasiticTentacles, "Parasitic Tentacles", 4));
        buyer.GainEnergy(5);
        seller.AddKeepCard(transferredCard);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyOwnedKeepCardCommand(seller.PlayerId, transferredCard.CardId, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, buyer.Energy);
        Assert.Equal(5, seller.Energy);
        Assert.Contains(buyer.KeepCards, card => card.CardId == KnownCardIds.ParasiticTentacles);
        Assert.Contains(buyer.KeepCards, card => card.CardId == transferredCard.CardId);
        Assert.DoesNotContain(seller.KeepCards, card => card.CardId == transferredCard.CardId);
        Assert.True(gameState.CurrentTurn.Flags.BoughtCard);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void BuyOwnedKeepCard_Should_ClearMimicTarget_WhenCopiedCardLeavesOriginalOwner()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var seller = gameState.GetPlayerById(1);
        var mimicOwner = gameState.GetPlayerById(2);
        var transferredCard = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        var mimic = CreateMimicCopying(seller.PlayerId, KnownCardIds.GiantBrain, "Giant Brain");

        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.ParasiticTentacles, "Parasitic Tentacles", 4));
        buyer.GainEnergy(5);
        seller.AddKeepCard(transferredCard);
        mimicOwner.AddKeepCard(mimic);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyOwnedKeepCardCommand(seller.PlayerId, transferredCard.CardId, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Null(mimic.MimicTarget);
        Assert.Contains(buyer.KeepCards, card => card.CardId == transferredCard.CardId);
        Assert.DoesNotContain(seller.KeepCards, card => card.CardId == transferredCard.CardId);
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
