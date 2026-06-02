using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class OpportunistReactionWindowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_CreateOpportunistPendingDecision_WhenNewCardIsRevealedAndEligiblePlayerCanPay()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var opportunistPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainEnergy(10);
        opportunistPlayer.GainEnergy(5);
        opportunistPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 4));

        var revealedCard = CreateKeepCard("card-revealed", "Revealed Card", 5);
        var engine = CreateEngine(
            CreateDiscardCard("card-slot-0", "Slot 0", 3),
            CreateDiscardCard("card-slot-1", "Slot 1", 3),
            CreateDiscardCard("card-slot-2", "Slot 2", 3),
            revealedCard);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.PendingDecision);
        Assert.Same(result.PendingDecision, gameState.PendingDecision);
        Assert.Equal(DecisionType.OpportunistPurchase, result.PendingDecision!.DecisionType);
        Assert.Equal(opportunistPlayer.PlayerId, result.PendingDecision.PlayerId);

        var payload = Assert.IsType<MarketCardRevealDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(0, payload.SlotIndex);
        Assert.Equal(revealedCard.CardId, payload.CardId);
        Assert.Equal(revealedCard.Name, payload.CardName);
        Assert.Equal(revealedCard.Cost, payload.Cost);
        Assert.Equal(new[] { opportunistPlayer.PlayerId }, payload.EligiblePlayerIds);
    }

    [Fact]
    public void RefreshMarket_Should_CreateOpportunistPendingDecision_WhenNewCardIsRevealedAndEligiblePlayerCanPay()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var opportunistPlayer = gameState.GetPlayerById(2);
        currentPlayer.GainEnergy(10);
        opportunistPlayer.GainEnergy(4);
        opportunistPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 4));

        var revealedCard = CreateKeepCard("card-refresh-revealed", "Refresh Revealed Card", 4);
        var engine = CreateEngine(
            CreateDiscardCard("card-slot-0", "Slot 0", 3),
            CreateDiscardCard("card-slot-1", "Slot 1", 3),
            CreateDiscardCard("card-slot-2", "Slot 2", 3),
            revealedCard,
            CreateDiscardCard("card-refresh-2", "Refresh 2", 3),
            CreateDiscardCard("card-refresh-3", "Refresh 3", 3));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new RefreshMarketCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.OpportunistPurchase, result.PendingDecision!.DecisionType);
        Assert.Equal(opportunistPlayer.PlayerId, result.PendingDecision.PlayerId);

        var payload = Assert.IsType<MarketCardRevealDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(0, payload.SlotIndex);
        Assert.Equal(revealedCard.CardId, payload.CardId);
        Assert.Equal(revealedCard.Name, payload.CardName);
        Assert.Equal(revealedCard.Cost, payload.Cost);
        Assert.Equal(new[] { opportunistPlayer.PlayerId }, payload.EligiblePlayerIds);
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotCreateOpportunistPendingDecision_WhenNoEligiblePlayerCanPay()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var opportunistPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainEnergy(10);
        opportunistPlayer.GainEnergy(2);
        opportunistPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 4));

        var engine = CreateEngine(
            CreateDiscardCard("card-slot-0", "Slot 0", 3),
            CreateDiscardCard("card-slot-1", "Slot 1", 3),
            CreateDiscardCard("card-slot-2", "Slot 2", 3),
            CreateKeepCard("card-expensive-revealed", "Expensive Revealed Card", 5));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Null(result.PendingDecision);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void DeclineOpportunistRevealedCard_Should_ClearPendingDecision_WhenActorOwnsReactionWindow()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var opportunistPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainEnergy(10);
        opportunistPlayer.GainEnergy(5);
        opportunistPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 4));

        var engine = CreateEngine(
            CreateDiscardCard("card-slot-0", "Slot 0", 3),
            CreateDiscardCard("card-slot-1", "Slot 1", 3),
            CreateDiscardCard("card-slot-2", "Slot 2", 3),
            CreateKeepCard("card-revealed", "Revealed Card", 5));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);
        var revealResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));
        Assert.True(revealResult.Success, revealResult.Error);
        Assert.NotNull(gameState.PendingDecision);

        var declineResult = engine.Execute(gameState, new DeclineOpportunistRevealedCardCommand(opportunistPlayer.PlayerId));

        Assert.True(declineResult.Success, declineResult.Error);
        Assert.Null(declineResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void BuyOpportunistRevealedCard_Should_BuyRevealedCardForOpportunistPlayer()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var opportunistPlayer = gameState.GetPlayerById(1);
        currentPlayer.GainEnergy(10);
        opportunistPlayer.GainEnergy(5);
        opportunistPlayer.AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 4));

        var revealedCard = CreateKeepCard("card-revealed", "Revealed Card", 5);
        var engine = CreateEngine(
            CreateDiscardCard("card-slot-0", "Slot 0", 3),
            CreateDiscardCard("card-slot-1", "Slot 1", 3),
            CreateDiscardCard("card-slot-2", "Slot 2", 3),
            revealedCard);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);
        var revealResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));
        Assert.True(revealResult.Success, revealResult.Error);
        Assert.NotNull(gameState.PendingDecision);

        var buyResult = engine.Execute(gameState, new BuyOpportunistRevealedCardCommand(opportunistPlayer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(0, opportunistPlayer.Energy);
        Assert.Contains(opportunistPlayer.KeepCards, card => card.CardId == revealedCard.CardId);
        Assert.Null(gameState.Market.FaceUpCards[0]);
        Assert.Null(gameState.PendingDecision);
        Assert.Contains(buyResult.NewEvents, e => e is CardBoughtEvent bought &&
                                                  bought.PlayerId == opportunistPlayer.PlayerId &&
                                                  bought.CardId == revealedCard.CardId &&
                                                  bought.EffectiveCost == revealedCard.Cost);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params MarketCardState[] starterDeck)
    {
        return new GameEngine(marketSetupService: new MarketSetupService(starterDeck));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(cardId, name, "Test keep card.", cost, MarketCardType.Keep);
    }

    private static MarketCardState CreateDiscardCard(string cardId, string name, int cost)
    {
        return new MarketCardState(cardId, name, "Test discard card.", cost, MarketCardType.Discard);
    }
}
