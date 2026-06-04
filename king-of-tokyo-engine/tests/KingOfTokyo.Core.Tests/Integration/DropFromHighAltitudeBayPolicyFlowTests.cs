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

public sealed class DropFromHighAltitudeBayPolicyFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_EnterBay_WhenCityIsOccupiedAndBayIsAvailableInFivePlayerGame()
    {
        var gameState = CreateGameState(5);
        var buyer = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        PutInCity(gameState, cityOccupant);
        buyer.GainEnergy(5);
        var engine = CreateEngineWithDropFromHighAltitudeInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(TokyoSlot.Bay, buyer.TokyoSlot);
        Assert.Equal(buyer.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                             entered.PlayerId == buyer.PlayerId &&
                                             entered.Slot == TokyoSlot.Bay);
        AssertDropDiscarded(gameState);
    }

    [Fact]
    public void BuyFaceUpCard_Should_EnterCity_WhenCityIsAvailableEvenIfBayIsOccupied()
    {
        var gameState = CreateGameState(5);
        var buyer = gameState.GetCurrentPlayer();
        var bayOccupant = gameState.GetPlayerById(2);
        PutInBay(gameState, bayOccupant);
        buyer.GainEnergy(5);
        var engine = CreateEngineWithDropFromHighAltitudeInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(TokyoSlot.City, buyer.TokyoSlot);
        Assert.Equal(buyer.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                             entered.PlayerId == buyer.PlayerId &&
                                             entered.Slot == TokyoSlot.City);
        AssertDropDiscarded(gameState);
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotEnterTokyo_WhenCityAndBayAreBothOccupied()
    {
        var gameState = CreateGameState(5);
        var buyer = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        PutInCity(gameState, cityOccupant);
        PutInBay(gameState, bayOccupant);
        buyer.GainEnergy(5);
        var engine = CreateEngineWithDropFromHighAltitudeInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(TokyoSlot.None, buyer.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.DoesNotContain(result.NewEvents, e => e is TokyoEnteredEvent);
        AssertDropDiscarded(gameState);
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotEnterTokyo_WhenCityIsOccupiedAndBayIsDisabledInFourPlayerGame()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        PutInCity(gameState, cityOccupant);
        buyer.GainEnergy(5);
        var engine = CreateEngineWithDropFromHighAltitudeInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(TokyoSlot.None, buyer.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.DoesNotContain(result.NewEvents, e => e is TokyoEnteredEvent);
        AssertDropDiscarded(gameState);
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotMovePlayer_WhenBuyerAlreadyOccupiesTokyo()
    {
        var gameState = CreateGameState(5);
        var buyer = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        PutInCity(gameState, cityOccupant);
        buyer.GainEnergy(5);
        var engine = CreateEngineWithDropFromHighAltitudeInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        PutInBay(gameState, buyer);
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(TokyoSlot.Bay, buyer.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(buyer.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.DoesNotContain(result.NewEvents, e => e is TokyoEnteredEvent);
        AssertDropDiscarded(gameState);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithDropFromHighAltitudeInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDropFromHighAltitude(),
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

    private static void PutInCity(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);
    }

    private static void PutInBay(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetBayOccupant(player.PlayerId);
    }

    private static void AssertDropDiscarded(GameState gameState)
    {
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.DropFromHighAltitude);
    }

    private static MarketCardState CreateDropFromHighAltitude()
    {
        return CreateDiscardCard(
            KnownCardIds.DropFromHighAltitude,
            "Drop from High Altitude",
            5,
            new CardPurchaseEffect
            {
                GainVictoryPoints = 2,
                EnterTokyo = true
            });
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
