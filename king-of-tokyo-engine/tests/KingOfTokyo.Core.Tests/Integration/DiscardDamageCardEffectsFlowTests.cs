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

public sealed class DiscardDamageCardEffectsFlowTests
{
    [Fact]
    public void BuyFireBlast_Should_DamageAllOtherMonsters()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(3);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.FireBlast,
                "Fire Blast",
                "All other monsters lose 2 health.",
                3,
                MarketCardType.Discard,
                new CardPurchaseEffect { DamageAllOthers = 2 }),
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
        Assert.Equal(0, player.Energy);
        Assert.Equal(10, player.Health);
        Assert.All(gameState.Players.Where(p => p.PlayerId != player.PlayerId), p => Assert.Equal(8, p.Health));
        Assert.Equal(3, result.NewEvents.OfType<DamageDealtEvent>().Count());
    }

    [Fact]
    public void BuyHighAltitudeBombing_Should_DamageAllMonsters_IncludingBuyer()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(4);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.HighAltitudeBombing,
                "High Altitude Bombing",
                "All monsters lose 3 health.",
                4,
                MarketCardType.Discard,
                new CardPurchaseEffect { DamageAllIncludingSelf = 3 }),
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
        Assert.All(gameState.Players, p => Assert.Equal(7, p.Health));
        Assert.Equal(4, result.NewEvents.OfType<DamageDealtEvent>().Count());
    }

    [Fact]
    public void BuyEvacuationOrders_Should_UseCardEffectDamage_AndNotCreateTokyoLeaveDecision()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetPlayerById(0);
        var tokyoDefender = gameState.GetPlayerById(1);

        player.GainEnergy(7);
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.EvacuationOrders,
                "Evacuation Orders",
                "All other monsters lose 5 health.",
                7,
                MarketCardType.Discard,
                new CardPurchaseEffect { DamageAllOthers = 5 }),
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
        Assert.Equal(5, tokyoDefender.Health);
        Assert.Null(result.PendingDecision);
        Assert.Equal(TokyoSlot.City, tokyoDefender.TokyoSlot);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == tokyoDefender.PlayerId &&
                                               damage.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void BuyFireBlast_Should_RespectArmorPlating()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        player.GainEnergy(3);
        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.FireBlast,
                "Fire Blast",
                "All other monsters lose 2 health.",
                3,
                MarketCardType.Discard,
                new CardPurchaseEffect { DamageAllOthers = 2 }),
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
        Assert.Equal(9, defender.Health);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 1);
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