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

public sealed class DefenseAndStatKeepCardEffectsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_IgnoreOneAttackDamage_WhenDefenderHasArmorPlating()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(10, defender.Health);
        Assert.Null(result.PendingDecision);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent);
    }

    [Fact]
    public void FinalizeDice_Should_IgnoreOneCardEffectDamage_WhenDefenderHasArmorPlating()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.PoisonQuills,
            "Poison Quills",
            "When you score 1s, also deal 2 damage.",
            3,
            MarketCardType.Keep));

        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(9, defender.Health);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 1 &&
                                               damage.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void BuyFaceUpCard_Should_IncreaseMaxHealth_AndHeal_WhenBuyingEvenBigger()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(8);
        player.TakeDamage(4);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.EvenBigger,
                "Even Bigger",
                "Your maximum health is increased by 2. When you gain this card, heal 2. When you lose this card, lose 2 health.",
                8,
                MarketCardType.Keep,
                new CardPurchaseEffect
                {
                    IncreaseMaxHealth = 2,
                    Heal = 2
                }),
            new MarketCardState("filler-001", "Filler 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-002", "Filler 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-003", "Filler 3", "No effect.", 1, MarketCardType.Keep)
        };

        var engine = new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(12, player.MaxHealth);
        Assert.Equal(8, player.Health);
        Assert.Equal(0, player.Energy);
        Assert.Single(player.KeepCards);
        Assert.Equal(KnownCardIds.EvenBigger, player.KeepCards[0].CardId);
    }

    [Fact]
    public void BuyFaceUpCard_Should_GrantPoints_AndHeal_WhenBuyingNuclearPowerPlant()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(6);
        player.TakeDamage(5);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.NuclearPowerPlant,
                "Nuclear Power Plant",
                "+2 victory points and heal 3 damage.",
                6,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    Heal = 3
                }),
            new MarketCardState("filler-001", "Filler 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-002", "Filler 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-003", "Filler 3", "No effect.", 1, MarketCardType.Keep)
        };

        var engine = new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Equal(8, player.Health);
        Assert.Equal(0, player.Energy);
        Assert.Empty(player.KeepCards);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.NuclearPowerPlant, gameState.Market.DiscardPile[0].CardId);

        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 2);

        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                               healed.PlayerId == player.PlayerId &&
                                               healed.Amount == 3);
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