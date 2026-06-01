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

public sealed class SelfDamageDiscardCardEffectsFlowTests
{
    [Fact]
    public void BuyNationalGuard_Should_GrantTwoPoints_AndDealTwoDamageToBuyer()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(3);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.NationalGuard,
                "National Guard",
                "+2 victory points and suffer 2 damage.",
                3,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 2,
                    DamageSelf = 2
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
        Assert.Equal(8, player.Health);
        Assert.Equal(0, player.Energy);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.TargetPlayerId == player.PlayerId &&
                                               damage.Amount == 2 &&
                                               damage.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void BuyTanks_Should_GrantFourPoints_AndDealThreeDamageToBuyer()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(4);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.Tanks,
                "Tanks",
                "+4 victory points and suffer 3 damage.",
                4,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 4,
                    DamageSelf = 3
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
        Assert.Equal(7, player.Health);
        Assert.Equal(0, player.Energy);
    }

    [Fact]
    public void BuyJetFighters_Should_GrantFivePoints_AndDealFourDamageToBuyer()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.GainEnergy(5);

        var deck = new[]
        {
            new MarketCardState(
                KnownCardIds.JetFighters,
                "Jet Fighters",
                "+5 victory points and suffer 4 damage.",
                5,
                MarketCardType.Discard,
                new CardPurchaseEffect
                {
                    GainVictoryPoints = 5,
                    DamageSelf = 4
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
        Assert.Equal(5, player.VictoryPoints);
        Assert.Equal(6, player.Health);
        Assert.Equal(0, player.Energy);
        Assert.Empty(player.KeepCards);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.JetFighters, gameState.Market.DiscardPile[0].CardId);
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