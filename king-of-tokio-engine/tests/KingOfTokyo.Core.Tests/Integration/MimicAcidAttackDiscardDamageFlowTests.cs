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

public sealed class MimicAcidAttackDiscardDamageFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_AddCopiedAcidAttackDamageToFireBlastAgainstOtherPlayers()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var acidOwner = gameState.GetPlayerById(1);
        var otherTarget = gameState.GetPlayerById(2);
        buyer.GainEnergy(3);
        acidOwner.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        buyer.AddKeepCard(CreateMimicCopying(acidOwner.PlayerId, KnownCardIds.AcidAttack, "Acid Attack"));
        var engine = CreateEngineWithFireBlastInSlotZero();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, acidOwner.Health);
        Assert.Equal(7, otherTarget.Health);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.Amount == 3 &&
            damage.DamageKind == DamageKind.CardEffect));
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
