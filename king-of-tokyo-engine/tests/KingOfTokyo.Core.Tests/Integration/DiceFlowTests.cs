using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class DiceFlowTests
{
    [Fact]
    public void RollDice_Should_RollAllDice_IncrementRollCount_AndReturnPendingDecision()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Energy, DieFace.Attack, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var result = engine.Execute(gameState, new RollDiceCommand(0));

        Assert.True(result.Success);
        Assert.Equal(1, gameState.CurrentTurn!.RollCountUsed);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, result.PendingDecision!.DecisionType);

        var faces = gameState.CurrentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray();
        Assert.Equal(
            new[] { DieFace.One, DieFace.Two, DieFace.Three, DieFace.Energy, DieFace.Attack, DieFace.Heart },
            faces);

        var rollEvent = Assert.Single(result.NewEvents.OfType<DiceRolledEvent>());
        Assert.Equal(1, rollEvent.RollNumber);
    }

    [Fact]
    public void RerollDice_Should_OnlyChangeSelectedDice_AndIncrementRollCount()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One, DieFace.One, DieFace.One, DieFace.One,
            DieFace.Attack, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new RerollDiceCommand(new[] { 1, 4 }, 0));

        Assert.True(result.Success);
        Assert.Equal(2, gameState.CurrentTurn!.RollCountUsed);

        var faces = gameState.CurrentTurn.DicePool.Dice.Select(d => d.CurrentFace).ToArray();
        Assert.Equal(DieFace.One, faces[0]);
        Assert.Equal(DieFace.Attack, faces[1]);
        Assert.Equal(DieFace.One, faces[2]);
        Assert.Equal(DieFace.One, faces[3]);
        Assert.Equal(DieFace.Heart, faces[4]);
        Assert.Equal(DieFace.One, faces[5]);

        var rollEvent = Assert.Single(result.NewEvents.OfType<DiceRolledEvent>());
        Assert.Equal(2, rollEvent.RollNumber);
    }

    [Fact]
    public void RerollDice_Should_NotReturnPendingDecision_AfterThirdRoll()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One, DieFace.One, DieFace.One, DieFace.One,
            DieFace.Two,
            DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new RerollDiceCommand(new[] { 0 }, 0));

        var result = engine.Execute(gameState, new RerollDiceCommand(new[] { 0 }, 0));

        Assert.True(result.Success);
        Assert.Equal(3, gameState.CurrentTurn!.RollCountUsed);
        Assert.Null(result.PendingDecision);
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