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
    public void Decline_Should_OfferRevealedCardToNextEligiblePlayer()
    {
        var gameState = CreateGameState(3);
        gameState.GetPlayerById(0).GainEnergy(5);
        foreach (var id in new[] { 1, 2 })
        {
            gameState.GetPlayerById(id).GainEnergy(5);
            gameState.GetPlayerById(id).AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 3));
        }
        var engine = CreateEngine(
            CreateDiscardCard("first", "First", 1),
            CreateDiscardCard("second", "Second", 1),
            CreateDiscardCard("third", "Third", 1),
            CreateDiscardCard("revealed", "Revealed", 2));
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);
        engine.Execute(gameState, new BuyFaceUpCardCommand(0, 0));

        Assert.Equal(1, gameState.PendingDecision?.PlayerId);
        var firstDecline = engine.Execute(gameState, new DeclineOpportunistRevealedCardCommand(1));
        Assert.True(firstDecline.Success, firstDecline.Error);
        Assert.Equal(2, gameState.PendingDecision?.PlayerId);
        var secondDecline = engine.Execute(gameState, new DeclineOpportunistRevealedCardCommand(2));
        Assert.True(secondDecline.Success, secondDecline.Error);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void Refresh_Should_OfferAllThreeRevealedCardsInOrder()
    {
        var gameState = CreateGameState(3);
        gameState.GetPlayerById(0).GainEnergy(2);
        gameState.GetPlayerById(1).GainEnergy(10);
        gameState.GetPlayerById(1).AddKeepCard(CreateKeepCard(KnownCardIds.Opportunist, "Opportunist", 3));
        var engine = CreateEngine(
            CreateDiscardCard("old-0", "Old 0", 1),
            CreateDiscardCard("old-1", "Old 1", 1),
            CreateDiscardCard("old-2", "Old 2", 1),
            CreateDiscardCard("new-0", "New 0", 2),
            CreateDiscardCard("new-1", "New 1", 2),
            CreateDiscardCard("new-2", "New 2", 2));
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        gameState.CurrentTurn!.SetPhase(TurnPhase.Purchase);

        var refresh = engine.Execute(gameState, new RefreshMarketCommand(0));
        Assert.True(refresh.Success, refresh.Error);
        for (var slot = 0; slot < 3; slot++)
        {
            var payload = Assert.IsType<MarketCardRevealDecisionData>(gameState.PendingDecision?.Payload);
            Assert.Equal(slot, payload.SlotIndex);
            Assert.True(engine.Execute(gameState, new DeclineOpportunistRevealedCardCommand(1)).Success);
        }
        Assert.Null(gameState.PendingDecision);
    }

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
                                                  bought.Cost == revealedCard.Cost);
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
