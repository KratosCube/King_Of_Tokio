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

public sealed class MoreSimpleDiscardCardEffectsFlowTests
{
    [Fact]
    public void BuyGasRefinery_Should_GrantTwoPoints_AndDamageAllOtherMonsters()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(6);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.GasRefinery,
                "Gas Refinery",
                "+2 victory points and deal 3 damage to all other monsters.",
                6,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    DamageAllOthers = 3
                }),
            new MarketCardState("filler-001", "Filler 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-002", "Filler 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-003", "Filler 3", "No effect.", 1, MarketCardType.Keep)
        };

        var engine = CreateEngineWithDeck(deck);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Equal(0, player.Energy);
        Assert.Equal(10, player.Health);
        Assert.All(gameState.Players.Where(p => p.PlayerId != player.PlayerId), p => Assert.Equal(7, p.Health));
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 2);
        Assert.Equal(3, result.NewEvents.OfType<DamageDealtEvent>().Count());
    }

    [Fact]
    public void BuySkyscraper_Should_GrantFourPoints_Only()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(6);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.Skyscraper,
                "Skyscraper",
                "+4 victory points.",
                6,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 4
                }),
            new MarketCardState("filler-001", "Filler 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-002", "Filler 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-003", "Filler 3", "No effect.", 1, MarketCardType.Keep)
        };

        var engine = CreateEngineWithDeck(deck);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(4, player.VictoryPoints);
        Assert.Equal(10, player.Health);
        Assert.Equal(0, player.Energy);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent);
    }

    [Fact]
    public void BuyVastStorm_Should_GrantTwoPoints_AndDamageOthers_BasedOnEnergy()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        var p1 = gameState.GetPlayerById(1);
        var p2 = gameState.GetPlayerById(2);
        var p3 = gameState.GetPlayerById(3);

        player.GainEnergy(6);
        p1.GainEnergy(5); // 2 damage
        p2.GainEnergy(2); // 1 damage
        p3.GainEnergy(1); // 0 damage

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.VastStorm,
                "Vast Storm",
                "+2 victory points. All other monsters lose 1 health for every 2 energy they have.",
                6,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    DamageOthersPerTwoEnergy = 1
                }),
            new MarketCardState("filler-001", "Filler 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-002", "Filler 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("filler-003", "Filler 3", "No effect.", 1, MarketCardType.Keep)
        };

        var engine = CreateEngineWithDeck(deck);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(0, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Equal(8, p1.Health);
        Assert.Equal(9, p2.Health);
        Assert.Equal(10, p3.Health);

        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == p1.PlayerId &&
                                               damage.Amount == 2);

        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == p2.PlayerId &&
                                               damage.Amount == 1);

        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                     damage.TargetPlayerId == p3.PlayerId);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithDeck(IReadOnlyList<MarketCardState> deck)
    {
        return new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));
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