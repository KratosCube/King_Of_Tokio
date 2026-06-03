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

public sealed class OmnivoreScoringFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GainOmnivoreBonus_WhenPlayerRollsAnyPair()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateKeepCard(KnownCardIds.Omnivore, "Omnivore", 4));

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == player.PlayerId &&
                                             gained.Amount == 2 &&
                                             gained.Reason == "Keep card: Omnivore.");
    }

    [Fact]
    public void FinalizeDice_Should_KeepNumberScoring_WhenOmnivorePairUsesSameDice()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateKeepCard(KnownCardIds.Omnivore, "Omnivore", 4));

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Energy, DieFace.Two);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(3, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == player.PlayerId &&
                                             gained.Amount == 1 &&
                                             gained.Reason == "Dice scoring.");
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == player.PlayerId &&
                                             gained.Amount == 2 &&
                                             gained.Reason == "Keep card: Omnivore.");
    }

    [Fact]
    public void FinalizeDice_Should_NotGainOmnivoreBonus_WhenPlayerRollsNoPair()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateKeepCard(KnownCardIds.Omnivore, "Omnivore", 4));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(1, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == player.PlayerId &&
                                             gained.Amount == 1 &&
                                             gained.Reason == "Entered Tokyo.");
        Assert.DoesNotContain(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.Reason == "Keep card: Omnivore.");
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] faces)
    {
        return new GameEngine(diceRollService: new DiceRollService(new SequenceRandomSource(faces)));
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
