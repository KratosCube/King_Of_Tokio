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

public sealed class MarketPurchaseFlowTests
{
    [Fact]
    public void EliminatedBuyer_CannotPurchaseAnotherCard()
    {
        var selfDamage = new MarketCardState(KnownCardIds.NationalGuard, "National Guard", "Self damage.", 3,
            MarketCardType.Discard, new CardPurchaseEffect { DamageSelf = 2 });
        var next = new MarketCardState("next", "Next", "Free card.", 0, MarketCardType.Keep);
        var game = CreateGameState(3);
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            selfDamage,
            new MarketCardState("other-1", "Other 1", "Other.", 0, MarketCardType.Keep),
            new MarketCardState("other-2", "Other 2", "Other.", 0, MarketCardType.Keep),
            next
        }));
        engine.Execute(game, new InitializeGameCommand());
        engine.Execute(game, new BeginTurnCommand(0));
        var player = game.GetCurrentPlayer();
        player.TakeDamage(9);
        player.GainEnergy(3);
        game.CurrentTurn!.MarkDiceResolved();
        game.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var first = engine.Execute(game, new BuyFaceUpCardCommand(0, 0));
        var second = engine.Execute(game, new BuyFaceUpCardCommand(0, 0));

        Assert.True(first.Success, first.Error);
        Assert.False(player.IsAlive);
        Assert.False(second.Success);
        Assert.Same(next, game.Market.FaceUpCards[0]);
    }

    [Fact]
    public void BuyMonsterBatteries_Should_StoreMatchingBankEnergyAfterPayment()
    {
        var battery = new MarketCardState(KnownCardIds.MonsterBatteries, "Monster Batteries", "Deposit energy.", 2, MarketCardType.Keep);
        var gameState = CreateGameState(3);
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[] { battery }));
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        gameState.GetCurrentPlayer().GainEnergy(5);
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, 0, storedEnergyToDeposit: 2));

        Assert.True(result.Success, result.Error);
        Assert.Equal(1, gameState.GetCurrentPlayer().Energy);
        Assert.Equal(4, battery.StoredEnergy);
        Assert.Contains(battery, gameState.GetCurrentPlayer().KeepCards);
    }

    [Fact]
    public void BuyFaceUpCard_Should_BuyKeepCard_AndKeepItOwned()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        var originalFaceUp = gameState.Market.FaceUpCards[0];

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, 0));

        Assert.True(result.Success);
        Assert.NotNull(originalFaceUp);
        Assert.Equal("Giant Brain", originalFaceUp!.Name);
        Assert.Equal(1, currentPlayer.Energy);
        Assert.Single(currentPlayer.KeepCards);
        Assert.Equal(originalFaceUp.CardId, currentPlayer.KeepCards[0].CardId);
        Assert.NotNull(gameState.Market.FaceUpCards[0]);
        Assert.NotEqual(originalFaceUp.CardId, gameState.Market.FaceUpCards[0]!.CardId);
        Assert.Contains(result.NewEvents, e => e is CardBoughtEvent bought &&
                                               bought.PlayerId == currentPlayer.PlayerId &&
                                               bought.CardId == originalFaceUp.CardId);
    }

    [Fact]
    public void BuyFaceUpCard_Should_RejectStoredEnergyAsPayment()
    {
        var testCard = new MarketCardState(
            "card-test-cost-5",
            "Cost 5 Card",
            "Test card.",
            5,
            MarketCardType.Keep);
        var gameState = CreateGameState(4);
        var marketSetupService = new MarketSetupService(new[]
        {
            testCard,
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { Heal = 2 }),
            new MarketCardState(KnownCardIds.ApartmentBuilding, "Apartment Building", "+3 victory points.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 3 })
        });
        var engine = CreateEngineWithMarket(marketSetupService,
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        var batteries = CreateMonsterBatteries(storedEnergy: 4);
        currentPlayer.AddKeepCard(batteries);

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));

        Assert.False(result.Success);
        Assert.Equal(3, currentPlayer.Energy);
        Assert.Equal(4, batteries.StoredEnergy);
        Assert.DoesNotContain(testCard, currentPlayer.KeepCards);
        Assert.Contains(batteries, currentPlayer.KeepCards);
        Assert.DoesNotContain(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.MonsterBatteries);
    }

    [Fact]
    public void BuyFaceUpCard_Should_NotConsumeBatteries_WhenPurchaseFails()
    {
        var testCard = new MarketCardState(
            "card-test-cost-5",
            "Cost 5 Card",
            "Test card.",
            5,
            MarketCardType.Keep);
        var gameState = CreateGameState(4);
        var marketSetupService = new MarketSetupService(new[]
        {
            testCard,
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { Heal = 2 }),
            new MarketCardState(KnownCardIds.ApartmentBuilding, "Apartment Building", "+3 victory points.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 3 })
        });
        var engine = CreateEngineWithMarket(marketSetupService,
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.AddKeepCard(CreateMonsterBatteries(storedEnergy: 2));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));

        Assert.False(result.Success);
        Assert.Equal(3, currentPlayer.Energy);
        Assert.DoesNotContain(testCard, currentPlayer.KeepCards);
        Assert.Contains(currentPlayer.KeepCards, card => card.CardId == KnownCardIds.MonsterBatteries && card.StoredEnergy == 2);
        Assert.Empty(gameState.Market.DiscardPile);
    }

    [Fact]
    public void BuyFaceUpCard_Should_ApplyHealDiscardEffect_AndDiscardTheCard()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.TakeDamage(4);

        var originalFaceUp = gameState.Market.FaceUpCards[1];

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(1, 0));

        Assert.True(result.Success);
        Assert.NotNull(originalFaceUp);
        Assert.Equal("Heal", originalFaceUp!.Name);
        Assert.Equal(8, currentPlayer.Health);
        Assert.Equal(3, currentPlayer.Energy);
        Assert.Empty(currentPlayer.KeepCards);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(originalFaceUp.CardId, gameState.Market.DiscardPile[0].CardId);
        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                               healed.PlayerId == currentPlayer.PlayerId &&
                                               healed.Amount == 2);
    }

    [Fact]
    public void BuyFaceUpCard_Should_ApplyVictoryPointDiscardEffect_AndDiscardTheCard()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        var originalFaceUp = gameState.Market.FaceUpCards[2];

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(2, 0));

        Assert.True(result.Success);
        Assert.NotNull(originalFaceUp);
        Assert.Equal("Apartment Building", originalFaceUp!.Name);
        Assert.Equal(3, currentPlayer.VictoryPoints);
        Assert.Equal(1, currentPlayer.Energy);
        Assert.Empty(currentPlayer.KeepCards);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(originalFaceUp.CardId, gameState.Market.DiscardPile[0].CardId);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == currentPlayer.PlayerId &&
                                               gained.Amount == 3);
    }

    [Fact]
    public void BuyFaceUpCard_Should_ApplyDropFromHighAltitudeEffect_AndEnterTokyo()
    {
        var dropCard = new MarketCardState(
            KnownCardIds.DropFromHighAltitude,
            "Drop from High Altitude",
            "+2 victory points and enter Tokyo if a slot is available.",
            5,
            MarketCardType.Discard,
            new CardPurchaseEffect
            {
                GainVictoryPoints = 2,
                EnterTokyo = true
            });

        var gameState = CreateGameState(4);
        var marketSetupService = new MarketSetupService(new[]
        {
            dropCard,
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { Heal = 2 }),
            new MarketCardState(KnownCardIds.ApartmentBuilding, "Apartment Building", "+3 victory points.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 3 })
        });
        var engine = CreateEngineWithMarket(marketSetupService,
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, 0));

        Assert.True(result.Success);
        Assert.Equal(2, currentPlayer.VictoryPoints);
        Assert.Equal(TokyoSlot.City, currentPlayer.TokyoSlot);
        Assert.Equal(currentPlayer.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                               entered.PlayerId == currentPlayer.PlayerId &&
                                               entered.Slot == TokyoSlot.City);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == currentPlayer.PlayerId &&
                                               gained.Amount == 2);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.DropFromHighAltitude, gameState.Market.DiscardPile[0].CardId);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] sequence)
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)));
    }

    private static GameEngine CreateEngineWithMarket(MarketSetupService marketSetupService, params DieFace[] sequence)
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)),
            marketSetupService: marketSetupService);
    }

    private static MarketCardState CreateMonsterBatteries(int storedEnergy)
    {
        return new MarketCardState(
            KnownCardIds.MonsterBatteries,
            "Monster Batteries",
            "Starts with 6 stored energy. At the end of each turn, lose 2 stored energy. Discard when empty.",
            5,
            MarketCardType.Keep,
            storedEnergy: storedEnergy);
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
