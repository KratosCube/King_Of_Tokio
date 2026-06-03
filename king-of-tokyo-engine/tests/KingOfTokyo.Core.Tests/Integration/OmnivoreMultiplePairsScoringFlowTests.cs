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

public sealed class OmnivoreMultiplePairsScoringFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GainSingleOmnivoreBonus_WhenPlayerRollsMultiplePairs()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateKeepCard(KnownCardIds.Omnivore, "Omnivore", 4));

        var engine = CreateEngine(
            DieFace.One, DieFace.One,
            DieFace.Two, DieFace.Two,
            DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, player.VictoryPoints);
        Assert.Single(result.NewEvents.OfType<VictoryPointsGainedEvent>(), gained =>
            gained.PlayerId == player.PlayerId &&
            gained.Amount == 2 &&
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
