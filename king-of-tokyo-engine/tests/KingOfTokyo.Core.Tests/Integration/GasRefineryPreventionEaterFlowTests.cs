using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class GasRefineryPreventionEaterFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_ApplyArmorPlatingAndCamouflageToAcidBoostedGasRefineryDamage_ThenAllowWingsCancellation()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var protectedTarget = gameState.GetPlayerById(1);
        var plainTarget = gameState.GetPlayerById(2);
        buyer.GainEnergy(6);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        protectedTarget.GainEnergy(2);
        protectedTarget.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating", 4));
        protectedTarget.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
        protectedTarget.AddKeepCard(CreateKeepCard(KnownCardIds.Wings, "Wings", 6));
        var engine = CreateEngineWithGasRefineryInSlotZero(new[]
        {
            DieFace.Heart,
            DieFace.Attack,
            DieFace.Attack
        });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(2, buyer.VictoryPoints);
        Assert.Equal(8, protectedTarget.Health);
        Assert.Equal(6, plainTarget.Health);
        Assert.True(gameState.CurrentTurn!.Flags.DealtDamage);
        Assert.Contains(buyResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                damage.SourcePlayerId == buyer.PlayerId &&
                                                damage.TargetPlayerId == protectedTarget.PlayerId &&
                                                damage.Amount == 2 &&
                                                damage.DamageKind == DamageKind.CardEffect);
        Assert.Contains(buyResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                damage.SourcePlayerId == buyer.PlayerId &&
                                                damage.TargetPlayerId == plainTarget.PlayerId &&
                                                damage.Amount == 4 &&
                                                damage.DamageKind == DamageKind.CardEffect);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(protectedTarget.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, protectedTarget.Health);
        Assert.Equal(0, protectedTarget.Energy);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == protectedTarget.PlayerId &&
                                                   canceled.Amount == 2 &&
                                                   canceled.Reason == "Keep card: Wings.");
    }

    [Fact]
    public void BuyFaceUpCard_Should_AwardEaterWhenGasRefineryEliminatesMonster()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var victim = gameState.GetPlayerById(1);
        var eaterOwner = gameState.GetPlayerById(2);
        buyer.GainEnergy(6);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        victim.TakeDamage(8);
        eaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        var engine = CreateEngineWithGasRefineryInSlotZero(Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(victim.IsAlive);
        Assert.True(eaterOwner.IsAlive);
        Assert.Equal(5, buyer.VictoryPoints);
        Assert.Equal(3, eaterOwner.VictoryPoints);
        Assert.True(gameState.CurrentTurn!.Flags.DealtDamage);
        Assert.True(gameState.CurrentTurn.Flags.EliminatedSomeone);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == buyer.PlayerId &&
                                             damage.TargetPlayerId == victim.PlayerId &&
                                             damage.Amount == 2 &&
                                             damage.DamageKind == DamageKind.CardEffect);
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                             eliminated.EliminatedPlayerId == victim.PlayerId &&
                                             eliminated.EliminatedByPlayerId == buyer.PlayerId &&
                                             eliminated.Reason == "Bought card: Gas Refinery.");
        Assert.Equal(2, result.NewEvents.OfType<VictoryPointsGainedEvent>().Count(gained =>
            gained.Amount == 3 &&
            gained.Reason == "Keep card: Eater of the Dead."));
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotDealGasRefineryDamageToBuyerEvenWithAcidAttack()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        buyer.GainEnergy(6);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        buyer.TakeDamage(4);
        var engine = CreateEngineWithGasRefineryInSlotZero(Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        MoveCurrentTurnToPurchase(gameState);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, buyer.Health);
        Assert.Equal(6, targetA.Health);
        Assert.Equal(6, targetB.Health);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.TargetPlayerId == buyer.PlayerId);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == buyer.PlayerId &&
            damage.Amount == 4 &&
            damage.DamageKind == DamageKind.CardEffect));
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithGasRefineryInSlotZero(IReadOnlyCollection<DieFace> camouflageFaces)
    {
        var keepCardRulesService = new KeepCardRulesService();
        var damagePreventionService = new DamagePreventionService(
            keepCardRulesService,
            new SequenceRandomSource(camouflageFaces));
        var damageApplier = new DamageApplier(
            keepCardRulesService,
            damagePreventionService);
        var marketPurchaseService = new MarketPurchaseService(
            keepCardRulesService: keepCardRulesService,
            damageApplier: damageApplier);

        return new GameEngine(
            marketSetupService: new MarketSetupService(CreateGasRefineryDeck()),
            marketPurchaseService: marketPurchaseService,
            keepCardRulesService: keepCardRulesService);
    }

    private static IReadOnlyList<MarketCardState> CreateGasRefineryDeck()
    {
        return new[]
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
        };
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

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(IEnumerable<DieFace> faces)
        {
            _faces = new Queue<DieFace>(faces);
        }

        public DieFace RollDieFace()
        {
            if (_faces.Count == 0)
            {
                throw new InvalidOperationException("No more queued die faces in SequenceRandomSource.");
            }

            return _faces.Dequeue();
        }
    }
}
