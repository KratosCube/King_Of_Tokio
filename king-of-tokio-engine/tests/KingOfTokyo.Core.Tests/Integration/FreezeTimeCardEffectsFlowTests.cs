using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class FreezeTimeCardEffectsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_ScheduleExtraTurnWithOneFewerDie_WhenPlayerScoresThreeOnesWithFreezeTime()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.FreezeTime,
            "Freeze Time",
            "After a turn in which you score three 1s, take one extra turn with one fewer die.",
            5,
            MarketCardType.Keep));
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Two, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(1, player.VictoryPoints);
        var scheduledTurn = Assert.Single(gameState.ScheduledTurns);
        Assert.Equal(player.PlayerId, scheduledTurn.PlayerId);
        Assert.Equal(-1, scheduledTurn.DiceCountModifier);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));
        Assert.True(endTurnResult.Success, endTurnResult.Error);

        var advanceResult = engine.Execute(gameState, new AdvanceToNextPlayerCommand());
        Assert.True(advanceResult.Success, advanceResult.Error);
        Assert.Equal(player.PlayerId, gameState.GetCurrentPlayer().PlayerId);
        Assert.Equal(-1, gameState.NextTurnDiceCountModifier);

        var beginExtraTurnResult = engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        Assert.True(beginExtraTurnResult.Success, beginExtraTurnResult.Error);
        Assert.NotNull(gameState.CurrentTurn);
        Assert.True(gameState.CurrentTurn!.IsExtraTurn);
        Assert.Equal(-1, gameState.CurrentTurn.DiceCountModifier);
        Assert.Equal(5, gameState.CurrentTurn.DiceCount);
        Assert.Empty(gameState.ScheduledTurns);
        Assert.Equal(0, gameState.NextTurnDiceCountModifier);
    }

    [Fact]
    public void FinalizeDice_Should_NotScheduleExtraTurn_WhenPlayerScoresThreeOnesWithoutFreezeTime()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Two, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(1, player.VictoryPoints);
        Assert.Empty(gameState.ScheduledTurns);
    }

    [Fact]
    public void FinalizeDice_Should_NotScheduleExtraTurn_WhenFreezeTimePlayerDoesNotScoreThreeOnes()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.FreezeTime,
            "Freeze Time",
            "After a turn in which you score three 1s, take one extra turn with one fewer die.",
            5,
            MarketCardType.Keep));
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.Two,
            DieFace.Two, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Empty(gameState.ScheduledTurns);
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
