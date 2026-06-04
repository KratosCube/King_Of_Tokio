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

public sealed class AcidAttackDiscardDamageFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_AddAcidAttackDamageToFireBlastAgainstOtherPlayers()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        buyer.GainEnergy(3);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, targetA.Health);
        Assert.Equal(7, targetB.Health);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.Amount == 3 &&
            damage.DamageKind == DamageKind.CardEffect));
    }

    [Fact]
    public void BuyFaceUpCard_Should_AddAcidAttackDamageToGasRefineryAgainstOtherPlayers()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        buyer.GainEnergy(6);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        var engine = CreateEngineWithGasRefineryInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, targetA.Health);
        Assert.Equal(6, targetB.Health);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.Amount == 4 &&
            damage.DamageKind == DamageKind.CardEffect));
    }

    [Fact]
    public void BuyFaceUpCard_Should_AddAcidAttackDamageToHighAltitudeBombingOnlyAgainstOtherPlayers()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        buyer.GainEnergy(4);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        var engine = CreateEngineWithHighAltitudeBombingInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, buyer.Health);
        Assert.Equal(6, targetA.Health);
        Assert.Equal(6, targetB.Health);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == buyer.PlayerId &&
                                             damage.TargetPlayerId == buyer.PlayerId &&
                                             damage.Amount == 3 &&
                                             damage.DamageKind == DamageKind.CardEffect);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.TargetPlayerId != buyer.PlayerId &&
            damage.Amount == 4 &&
            damage.DamageKind == DamageKind.CardEffect));
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotAddAcidAttackDamageToSelfDamage()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        buyer.GainEnergy(4);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        var engine = CreateEngineWithTanksInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, buyer.Health);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == buyer.PlayerId &&
                                             damage.TargetPlayerId == buyer.PlayerId &&
                                             damage.Amount == 3 &&
                                             damage.DamageKind == DamageKind.CardEffect);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithFireBlastInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(
                KnownCardIds.FireBlast,
                "Fire Blast",
                3,
                new CardPurchaseEffect { DamageAllOthers = 2 }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        }));
    }

    private static GameEngine CreateEngineWithGasRefineryInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(
                KnownCardIds.GasRefinery,
                "Gas Refinery",
                6,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    DamageAllOthers = 3
                }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        }));
    }

    private static GameEngine CreateEngineWithHighAltitudeBombingInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(
                KnownCardIds.HighAltitudeBombing,
                "High Altitude Bombing",
                4,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    DamageAllIncludingSelf = 3
                }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        }));
    }

    private static GameEngine CreateEngineWithTanksInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(
                KnownCardIds.Tanks,
                "Tanks",
                4,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 4,
                    DamageSelf = 3
                }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        }));
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
