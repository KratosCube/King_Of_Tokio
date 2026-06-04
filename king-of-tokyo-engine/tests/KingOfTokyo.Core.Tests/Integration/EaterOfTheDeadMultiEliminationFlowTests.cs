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

public sealed class EaterOfTheDeadMultiEliminationFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_AwardEachAliveEaterOwnerForEachMonsterEliminatedBySameFireBlast()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var eliminatedEaterOwner = gameState.GetPlayerById(1);
        var eliminatedPlainMonster = gameState.GetPlayerById(2);
        var survivingEaterOwner = gameState.GetPlayerById(3);
        buyer.GainEnergy(3);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        eliminatedEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        survivingEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        eliminatedEaterOwner.TakeDamage(8);
        eliminatedPlainMonster.TakeDamage(8);
        var engine = CreateEngineWithMarket(CreateFireBlastDeck());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(eliminatedEaterOwner.IsAlive);
        Assert.False(eliminatedPlainMonster.IsAlive);
        Assert.True(survivingEaterOwner.IsAlive);
        Assert.Equal(6, buyer.VictoryPoints);
        Assert.Equal(0, eliminatedEaterOwner.VictoryPoints);
        Assert.Equal(6, survivingEaterOwner.VictoryPoints);
        Assert.True(gameState.CurrentTurn.Flags.EliminatedSomeone);
        Assert.Equal(2, result.NewEvents.OfType<PlayerEliminatedEvent>().Count(eliminated =>
            eliminated.EliminatedByPlayerId == buyer.PlayerId));
        Assert.Equal(4, result.NewEvents.OfType<VictoryPointsGainedEvent>().Count(gained =>
            gained.Amount == 3 &&
            gained.Reason == "Keep card: Eater of the Dead."));
        Assert.DoesNotContain(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.PlayerId == eliminatedEaterOwner.PlayerId &&
                                                    gained.Reason == "Keep card: Eater of the Dead.");
    }

    [Fact]
    public void EndTurn_Should_FinishWithNoWinner_WhenEaterOwnerReachesTwentyDuringAllMonstersEliminatedCardEffect()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var firstVictim = gameState.GetPlayerById(1);
        var eaterOwnerWhoLaterDies = gameState.GetPlayerById(2);
        buyer.GainEnergy(4);
        buyer.TakeDamage(7);
        firstVictim.TakeDamage(7);
        eaterOwnerWhoLaterDies.TakeDamage(7);
        eaterOwnerWhoLaterDies.GainVictoryPoints(17);
        eaterOwnerWhoLaterDies.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        var engine = CreateEngineWithMarket(CreateHighAltitudeBombingDeck());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.False(buyer.IsAlive);
        Assert.False(firstVictim.IsAlive);
        Assert.False(eaterOwnerWhoLaterDies.IsAlive);
        Assert.True(eaterOwnerWhoLaterDies.VictoryPoints >= 20);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.False(gameState.WinnerInfo!.HasWinner);
        Assert.Null(gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("All monsters were eliminated.", gameState.WinnerInfo.Reason);
        Assert.Contains(buyResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                 gained.PlayerId == eaterOwnerWhoLaterDies.PlayerId &&
                                                 gained.Amount == 3 &&
                                                 gained.Reason == "Keep card: Eater of the Dead.");
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                     ended.WinnerPlayerId is null &&
                                                     ended.Reason == "All monsters were eliminated.");
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithMarket(IReadOnlyList<MarketCardState> deck)
    {
        return new GameEngine(marketSetupService: new MarketSetupService(deck));
    }

    private static IReadOnlyList<MarketCardState> CreateFireBlastDeck()
    {
        return new[]
        {
            CreateDiscardCard(KnownCardIds.FireBlast, "Fire Blast", 3, new CardPurchaseEffect { DamageAllOthers = 2 }),
            CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 })
        };
    }

    private static IReadOnlyList<MarketCardState> CreateHighAltitudeBombingDeck()
    {
        return new[]
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
        };
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
