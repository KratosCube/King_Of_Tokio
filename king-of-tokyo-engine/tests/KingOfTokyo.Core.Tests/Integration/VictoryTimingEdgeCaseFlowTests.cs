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

public sealed class VictoryTimingEdgeCaseFlowTests
{
    [Fact]
    public void EndTurn_Should_PrioritizeCurrentPlayer_WhenCurrentAndNonCurrentReachTwentyFromSameEaterResolution()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var victimA = gameState.GetPlayerById(1);
        var nonCurrentEaterOwner = gameState.GetPlayerById(2);
        var victimB = gameState.GetPlayerById(3);
        buyer.GainEnergy(3);
        buyer.GainVictoryPoints(17);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        nonCurrentEaterOwner.GainVictoryPoints(17);
        nonCurrentEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        victimA.TakeDamage(8);
        victimB.TakeDamage(8);
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);
        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(23, buyer.VictoryPoints);
        Assert.Equal(23, nonCurrentEaterOwner.VictoryPoints);
        Assert.False(victimA.IsAlive);
        Assert.False(victimB.IsAlive);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(buyer.PlayerId));

        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(buyer.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", gameState.WinnerInfo.Reason);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                     ended.WinnerPlayerId == buyer.PlayerId &&
                                                     ended.Reason == "Reached 20 victory points.");
    }

    [Fact]
    public void EndTurn_Should_SelectFirstAliveNonCurrentPlayer_WhenMultipleNonCurrentPlayersReachTwentyFromSameEaterResolution()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var victim = gameState.GetPlayerById(1);
        var firstEaterOwner = gameState.GetPlayerById(2);
        var secondEaterOwner = gameState.GetPlayerById(3);
        buyer.GainEnergy(3);
        firstEaterOwner.GainVictoryPoints(17);
        firstEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        secondEaterOwner.GainVictoryPoints(17);
        secondEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        victim.TakeDamage(8);
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);
        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(0, buyer.VictoryPoints);
        Assert.Equal(20, firstEaterOwner.VictoryPoints);
        Assert.Equal(20, secondEaterOwner.VictoryPoints);
        Assert.False(victim.IsAlive);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(buyer.PlayerId));

        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(firstEaterOwner.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", gameState.WinnerInfo.Reason);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                     ended.WinnerPlayerId == firstEaterOwner.PlayerId &&
                                                     ended.Reason == "Reached 20 victory points.");
    }

    [Fact]
    public void EndTurn_Should_FinishWithNoWinner_WhenAllMonstersAreEliminatedEvenIfPlayersReachedTwentyEarlierInTurn()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var victimA = gameState.GetPlayerById(1);
        var victimB = gameState.GetPlayerById(2);
        buyer.GainEnergy(4);
        buyer.GainVictoryPoints(18);
        buyer.TakeDamage(7);
        victimA.TakeDamage(7);
        victimB.TakeDamage(7);
        var engine = CreateEngineWithHighAltitudeBombingInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);
        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(20, buyer.VictoryPoints);
        Assert.False(buyer.IsAlive);
        Assert.False(victimA.IsAlive);
        Assert.False(victimB.IsAlive);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(buyer.PlayerId));

        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.False(gameState.WinnerInfo!.HasWinner);
        Assert.Null(gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("All monsters were eliminated.", gameState.WinnerInfo.Reason);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                     ended.WinnerPlayerId is null &&
                                                     ended.Reason == "All monsters were eliminated.");
    }

    [Fact]
    public void EndTurn_Should_FinishWithNoWinner_InLastMonsterStandingMode_WhenAllMonstersAreEliminated()
    {
        var gameState = CreateGameState(3, VictoryMode.LastMonsterStanding);
        var buyer = gameState.GetCurrentPlayer();
        var victimA = gameState.GetPlayerById(1);
        var victimB = gameState.GetPlayerById(2);
        buyer.GainEnergy(4);
        buyer.GainVictoryPoints(20);
        buyer.TakeDamage(7);
        victimA.TakeDamage(7);
        victimB.TakeDamage(7);
        var engine = CreateEngineWithHighAltitudeBombingInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);
        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.False(buyer.IsAlive);
        Assert.False(victimA.IsAlive);
        Assert.False(victimB.IsAlive);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(buyer.PlayerId));

        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.False(gameState.WinnerInfo!.HasWinner);
        Assert.Null(gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("All monsters were eliminated.", gameState.WinnerInfo.Reason);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                     ended.WinnerPlayerId is null &&
                                                     ended.Reason == "All monsters were eliminated.");
    }

    private static GameState CreateGameState(int playerCount, VictoryMode victoryMode = VictoryMode.Standard)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount, victoryMode));
    }

    private static GameEngine CreateEngineWithFireBlastInSlotZero()
    {
        return new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            CreateDiscardCard(KnownCardIds.FireBlast, "Fire Blast", 3, new CardPurchaseEffect { DamageAllOthers = 2 }),
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
