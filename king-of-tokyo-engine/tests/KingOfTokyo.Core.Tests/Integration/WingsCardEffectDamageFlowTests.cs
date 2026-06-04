using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class WingsCardEffectDamageFlowTests
{
    [Fact]
    public void ActivateWings_Should_CancelBoughtFireBlastDamageTakenByNonCurrentPlayer()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var wingsOwner = gameState.GetPlayerById(1);
        var otherTarget = gameState.GetPlayerById(2);
        buyer.GainEnergy(3);
        wingsOwner.GainEnergy(2);
        wingsOwner.AddKeepCard(CreateKeepCard(KnownCardIds.Wings, "Wings", 6));
        var engine = CreateEngineWithMarket(CreateFireBlastDeck());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(8, wingsOwner.Health);
        Assert.Equal(8, otherTarget.Health);
        Assert.Equal(2, wingsOwner.Energy);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(wingsOwner.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, wingsOwner.Health);
        Assert.Equal(0, wingsOwner.Energy);
        Assert.Equal(8, otherTarget.Health);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == wingsOwner.PlayerId &&
                                                   canceled.Amount == 2);
    }

    [Fact]
    public void ActivateWings_Should_CancelBoughtHighAltitudeBombingSelfDamageForCurrentPlayer()
    {
        var gameState = CreateGameState(3);
        var buyer = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        buyer.GainEnergy(6);
        buyer.AddKeepCard(CreateKeepCard(KnownCardIds.Wings, "Wings", 6));
        var engine = CreateEngineWithMarket(CreateHighAltitudeBombingDeck());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(buyer.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, buyer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(7, buyer.Health);
        Assert.Equal(7, targetA.Health);
        Assert.Equal(7, targetB.Health);
        Assert.Equal(2, buyer.Energy);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(buyer.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, buyer.Health);
        Assert.Equal(0, buyer.Energy);
        Assert.Equal(7, targetA.Health);
        Assert.Equal(7, targetB.Health);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == buyer.PlayerId &&
                                                   canceled.Amount == 3);
    }

    [Fact]
    public void ActivateWings_Should_CancelPoisonQuillsCardEffectDamageTakenDuringDiceResolution()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var wingsOwner = gameState.GetPlayerById(1);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.PoisonQuills, "Poison Quills", 3));
        wingsOwner.GainEnergy(2);
        wingsOwner.AddKeepCard(CreateKeepCard(KnownCardIds.Wings, "Wings", 6));
        PutInCity(gameState, wingsOwner);
        var engine = CreateDiceEngine(
            DieFace.One,
            DieFace.One,
            DieFace.One,
            DieFace.Heart,
            DieFace.Two,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, wingsOwner.Health);
        Assert.Equal(2, wingsOwner.Energy);
        Assert.Contains(finalizeResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                     damage.TargetPlayerId == wingsOwner.PlayerId &&
                                                     damage.Amount == 2 &&
                                                     damage.DamageKind == DamageKind.CardEffect);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(wingsOwner.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, wingsOwner.Health);
        Assert.Equal(0, wingsOwner.Energy);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == wingsOwner.PlayerId &&
                                                   canceled.Amount == 2);
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

    private static GameEngine CreateDiceEngine(params DieFace[] sequence)
    {
        return new GameEngine(diceRollService: new DiceRollService(new SequenceRandomSource(sequence)));
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

    private static void PutInCity(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);
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
