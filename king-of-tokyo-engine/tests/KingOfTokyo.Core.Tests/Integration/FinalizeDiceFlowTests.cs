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

public sealed class FinalizeDiceFlowTests
{
    [Fact]
    public void FinalizeDice_Should_Fail_WhenNoRollWasMade()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Attack, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void FinalizeDice_Should_GrantTwoPoints_WhenPlayerScoresThreeOnes_AndEntersEmptyTokyo()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Attack, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();

        Assert.True(result.Success);
        Assert.True(gameState.CurrentTurn!.DiceResolved);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn.Phase);
        Assert.Equal(2, currentPlayer.VictoryPoints);
        Assert.Equal(TokyoSlot.City, currentPlayer.TokyoSlot);
        Assert.Contains(result.NewEvents, e => e is DiceFinalizedEvent);
        Assert.Equal(
            2,
            result.NewEvents.OfType<VictoryPointsGainedEvent>().Sum(e => e.Amount));
    }

    [Fact]
    public void FinalizeDice_Should_GrantThreePoints_ForFourTwos_WhenNoAttackIsRolled()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Two,
            DieFace.Two, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();

        Assert.True(result.Success);
        Assert.Equal(3, currentPlayer.VictoryPoints);
        Assert.Equal(TokyoSlot.None, currentPlayer.TokyoSlot);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
    }

    [Fact]
    public void FinalizeDice_Should_RemovePoisonTokensBeforeHealing()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetPlayerById(0);
        currentPlayer.TakeDamage(3);
        currentPlayer.Status.AddPoisonTokens(2);
        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        Assert.True(result.Success);
        Assert.Equal(0, currentPlayer.Status.PoisonTokens);
        Assert.Equal(8, currentPlayer.Health);

        var statusEvent = Assert.Single(result.NewEvents.OfType<StatusTokensRemovedEvent>());
        Assert.Equal(2, statusEvent.PoisonTokensRemoved);
        Assert.Equal(0, statusEvent.ShrinkTokensRemoved);
        Assert.Equal(2, statusEvent.HeartsSpent);
    }

    [Fact]
    public void FinalizeDice_Should_RemoveShrinkTokensAfterPoisonTokens()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetPlayerById(0);
        currentPlayer.Status.AddPoisonTokens(1);
        currentPlayer.Status.AddShrinkTokens(2);
        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.One,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        Assert.True(result.Success);
        Assert.Equal(0, currentPlayer.Status.PoisonTokens);
        Assert.Equal(1, currentPlayer.Status.ShrinkTokens);

        var statusEvent = Assert.Single(result.NewEvents.OfType<StatusTokensRemovedEvent>());
        Assert.Equal(1, statusEvent.PoisonTokensRemoved);
        Assert.Equal(1, statusEvent.ShrinkTokensRemoved);
        Assert.Equal(2, statusEvent.HeartsSpent);
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
