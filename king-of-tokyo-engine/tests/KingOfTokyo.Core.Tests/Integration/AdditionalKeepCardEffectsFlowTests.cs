using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class AdditionalKeepCardEffectsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GiveOneExtraEnergy_WhenPlayerHasFriendOfChildren()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.FriendOfChildren,
            "Friend of Children",
            "If you gain any energy, gain 1 extra energy.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(3, player.Energy);
        Assert.Contains(result.NewEvents, e => e is EnergyGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 3);
    }

    [Fact]
    public void BuyEnergize_Should_GiveOneExtraEnergy_WhenPlayerHasFriendOfChildren()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.FriendOfChildren,
            "Friend of Children",
            "If you gain any energy, gain 1 extra energy.",
            3,
            MarketCardType.Keep));

        player.GainEnergy(8);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.Energize,
                "Energize",
                "+9 energy.",
                8,
                MarketCardType.Discard,
                new CardPurchaseEffect { GainEnergy = 9 }),
            new MarketCardState(
                "filler-001",
                "Filler 1",
                "No effect.",
                1,
                MarketCardType.Keep),
            new MarketCardState(
                "filler-002",
                "Filler 2",
                "No effect.",
                1,
                MarketCardType.Keep),
            new MarketCardState(
                "filler-003",
                "Filler 3",
                "No effect.",
                1,
                MarketCardType.Keep)
        };

        var engine = new GameEngine(
            marketSetupService: new Services.MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.Heart, DieFace.Heart, DieFace.Attack
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(10, player.Energy);
        Assert.Contains(result.NewEvents, e => e is EnergyGainedEvent gained &&
                                               gained.Amount == 10);
    }

    [Fact]
    public void BeginTurn_Should_GiveOneExtraVictoryPoint_WhenPlayerHasUrbavoreAndStartsInTokyo()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Urbavore,
            "Urbavore",
            "When you start your turn in Tokyo, gain 1 extra victory point. When you deal any damage from Tokyo, deal 1 extra damage.",
            4,
            MarketCardType.Keep));

        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        var result = engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(3, player.VictoryPoints);
        Assert.Equal(2, result.NewEvents.OfType<VictoryPointsGainedEvent>().Count());
        Assert.Equal(3, result.NewEvents.OfType<VictoryPointsGainedEvent>().Sum(e => e.Amount));
    }

    [Fact]
    public void FinalizeDice_Should_DealOneExtraDamageFromTokyo_WhenPlayerHasUrbavore()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(attacker.PlayerId);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.Urbavore,
            "Urbavore",
            "When you start your turn in Tokyo, gain 1 extra victory point. When you deal any damage from Tokyo, deal 1 extra damage.",
            4,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
    }

    [Fact]
    public void ActivateRapidHealing_Should_SpendTwoEnergy_AndHealOne()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.TakeDamage(4);
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.RapidHealing,
            "Rapid Healing",
            "Any time during your turn, spend 2 energy to heal 1 damage.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateRapidHealingCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, player.Energy);
        Assert.Equal(7, player.Health);
        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                               healed.PlayerId == player.PlayerId &&
                                               healed.Amount == 1);
    }

    [Fact]
    public void EndTurn_Should_GainOneEnergy_WhenPlayerHasSolarPowered_AndHasNoEnergy()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.SolarPowered,
            "Solar Powered",
            "At the end of your turn, gain 1 energy if you have none.",
            2,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, player.Energy);
        Assert.Contains(result.NewEvents, e => e is EnergyGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 1);
    }

    [Fact]
    public void EndTurn_Should_GainVictoryPointsFromEnergyHoarder()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.GainEnergy(12);
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.EnergyHoarder,
            "Energy Hoarder",
            "Gain 1 victory point for every 6 energy you have at the end of your turn.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 2);
    }

    [Fact]
    public void EndTurn_Should_GainVictoryPointFromRootingForTheUnderdog_WhenPlayerHasFewestPoints()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        gameState.GetPlayerById(1).GainVictoryPoints(2);
        gameState.GetPlayerById(2).GainVictoryPoints(1);

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.RootingForTheUnderdog,
            "Rooting for the Underdog",
            "At the end of your turn, if you have the fewest victory points, gain 1 victory point.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 1);
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