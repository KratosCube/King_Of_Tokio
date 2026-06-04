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

public sealed class AcidAttackFireBlastEaterFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_AwardEaterForEachMonsterEliminatedByAcidBoostedFireBlast()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var eliminatedA = gameState.GetPlayerById(1);
        var eliminatedB = gameState.GetPlayerById(2);
        var survivingEaterOwner = gameState.GetPlayerById(3);
        buyer.GainEnergy(3);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        survivingEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        eliminatedA.TakeDamage(7);
        eliminatedB.TakeDamage(7);
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(eliminatedA.IsAlive);
        Assert.False(eliminatedB.IsAlive);
        Assert.Equal(7, survivingEaterOwner.Health);
        Assert.Equal(6, buyer.VictoryPoints);
        Assert.Equal(6, survivingEaterOwner.VictoryPoints);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.True(gameState.CurrentTurn.Flags.EliminatedSomeone);
        Assert.Equal(3, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.Amount == 3 &&
            damage.DamageKind == DamageKind.CardEffect));
        Assert.Equal(2, result.NewEvents.OfType<PlayerEliminatedEvent>().Count(eliminated =>
            eliminated.EliminatedByPlayerId == buyer.PlayerId));
        Assert.Equal(4, result.NewEvents.OfType<VictoryPointsGainedEvent>().Count(gained =>
            gained.Amount == 3 &&
            gained.Reason == "Keep card: Eater of the Dead."));
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotAwardDeadEaterOwner_WhenEliminatedBySameAcidBoostedFireBlast()
    {
        var gameState = CreateGameState(4);
        var buyer = gameState.GetCurrentPlayer();
        var eliminatedEaterOwner = gameState.GetPlayerById(1);
        var eliminatedPlainMonster = gameState.GetPlayerById(2);
        var survivingPlainMonster = gameState.GetPlayerById(3);
        buyer.GainEnergy(3);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        eliminatedEaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        eliminatedEaterOwner.TakeDamage(7);
        eliminatedPlainMonster.TakeDamage(7);
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(eliminatedEaterOwner.IsAlive);
        Assert.False(eliminatedPlainMonster.IsAlive);
        Assert.Equal(7, survivingPlainMonster.Health);
        Assert.Equal(0, buyer.VictoryPoints);
        Assert.Equal(0, eliminatedEaterOwner.VictoryPoints);
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
