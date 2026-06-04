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

public sealed class EvacuationOrdersEaterChildFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_AwardAliveEaterOwnersForEvacuationOrdersEliminationsIncludingItHasAChildReplacement()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var childVictim = gameState.GetPlayerById(1);
        var plainVictim = gameState.GetPlayerById(2);
        var observer = gameState.GetPlayerById(3);
        buyer.GainEnergy(7);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        childVictim.TakeDamage(5);
        childVictim.GainEnergy(4);
        childVictim.AddKeepCard(CreateKeepCard(KnownCardIds.ItHasAChild, "It Has a Child", 7));
        childVictim.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        plainVictim.TakeDamage(5);
        observer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        var engine = CreateEngineWithEvacuationOrdersInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.True(childVictim.IsAlive);
        Assert.False(plainVictim.IsAlive);
        Assert.True(observer.IsAlive);
        Assert.Equal(10, childVictim.Health);
        Assert.Equal(0, childVictim.Energy);
        Assert.Empty(childVictim.KeepCards);
        Assert.Equal(5, observer.Health);
        Assert.Equal(6, buyer.VictoryPoints);
        Assert.Equal(0, childVictim.VictoryPoints);
        Assert.Equal(6, observer.VictoryPoints);
        Assert.True(gameState.CurrentTurn!.Flags.DealtDamage);
        Assert.True(gameState.CurrentTurn.Flags.EliminatedSomeone);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.ItHasAChild);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.EaterOfTheDead);
        Assert.Equal(2, result.NewEvents.OfType<PlayerEliminatedEvent>().Count(eliminated =>
            eliminated.EliminatedByPlayerId == buyer.PlayerId &&
            eliminated.Reason == "Bought card: Evacuation Orders."));
        Assert.Equal(4, result.NewEvents.OfType<VictoryPointsGainedEvent>().Count(gained =>
            gained.Amount == 3 &&
            gained.Reason == "Keep card: Eater of the Dead."));
        Assert.DoesNotContain(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.PlayerId == childVictim.PlayerId &&
                                                    gained.Reason == "Keep card: Eater of the Dead.");
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotAwardEaterOwnerEliminatedBySameEvacuationOrders()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var eliminatedEaterOwner = gameState.GetPlayerById(1);
        var childVictim = gameState.GetPlayerById(2);
        buyer.GainEnergy(7);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        eliminatedEaterOwner.TakeDamage(5);
        eliminatedEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        childVictim.TakeDamage(5);
        childVictim.AddKeepCard(CreateKeepCard(KnownCardIds.ItHasAChild, "It Has a Child", 7));
        var engine = CreateEngineWithEvacuationOrdersInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(eliminatedEaterOwner.IsAlive);
        Assert.True(childVictim.IsAlive);
        Assert.Equal(10, childVictim.Health);
        Assert.Equal(6, buyer.VictoryPoints);
        Assert.Equal(0, eliminatedEaterOwner.VictoryPoints);
        Assert.Equal(2, result.NewEvents.OfType<PlayerEliminatedEvent>().Count(eliminated =>
            eliminated.EliminatedByPlayerId == buyer.PlayerId &&
            eliminated.Reason == "Bought card: Evacuation Orders."));
        Assert.DoesNotContain(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.PlayerId == eliminatedEaterOwner.PlayerId &&
                                                    gained.Reason == "Keep card: Eater of the Dead.");
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithEvacuationOrdersInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(KnownCardIds.EvacuationOrders, "Evacuation Orders", 7, new CardPurchaseEffect { DamageAllOthers = 5 }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        }));
    }

    private static void MoveCurrentTurnToPurchase(GameState gameState)
    {
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);
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

    private static MarketCardState CreateDiscardCard(string cardId, string name, int cost, CardPurchaseEffect purchaseEffect)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test discard card.",
            cost,
            MarketCardType.Discard,
            purchaseEffect);
    }
}
