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

public sealed class MarketPurchaseFlowTests
{
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