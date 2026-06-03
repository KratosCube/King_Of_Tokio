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

public sealed class StateSafetyRegressionFlowTests
{
    [Fact]
    public void FailedRollDice_Should_NotChangeState_WhenNoTurnIsActive()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        var versionBeforeFailure = gameState.Version;
        var eventCountBeforeFailure = gameState.EventLog.Count;

        var failedRollResult = engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.False(failedRollResult.Success);
        Assert.Equal("Cannot roll dice without an active turn.", failedRollResult.Error);
        Assert.Same(gameState, failedRollResult.GameState);
        Assert.Empty(failedRollResult.NewEvents);
        Assert.Null(failedRollResult.PendingDecision);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventCountBeforeFailure, gameState.EventLog.Count);
        Assert.Null(gameState.CurrentTurn);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void FailedEndTurn_Should_NotChangeState_WhenTurnIsStillRolling()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        var versionBeforeFailure = gameState.Version;
        var eventCountBeforeFailure = gameState.EventLog.Count;

        var failedEndTurnResult = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.False(failedEndTurnResult.Success);
        Assert.Equal("Turn can only be ended from the purchase phase.", failedEndTurnResult.Error);
        Assert.Same(gameState, failedEndTurnResult.GameState);
        Assert.Empty(failedEndTurnResult.NewEvents);
        Assert.Null(failedEndTurnResult.PendingDecision);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventCountBeforeFailure, gameState.EventLog.Count);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn!.Phase);
        Assert.Null(gameState.PendingDecision);
    }

    [Fact]
    public void FailedEndTurn_Should_NotClearPendingDecision_WhenRerollDecisionIsPending()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));
        var pendingDecisionBeforeFailure = gameState.PendingDecision;
        var versionBeforeFailure = gameState.Version;
        var eventCountBeforeFailure = gameState.EventLog.Count;

        var failedEndTurnResult = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.NotNull(pendingDecisionBeforeFailure);
        Assert.Equal(DecisionType.SelectDiceToReroll, pendingDecisionBeforeFailure!.DecisionType);
        Assert.False(failedEndTurnResult.Success);
        Assert.Equal("Cannot end turn while another decision is pending.", failedEndTurnResult.Error);
        Assert.Same(gameState, failedEndTurnResult.GameState);
        Assert.Empty(failedEndTurnResult.NewEvents);
        Assert.Null(failedEndTurnResult.PendingDecision);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventCountBeforeFailure, gameState.EventLog.Count);
        Assert.Same(pendingDecisionBeforeFailure, gameState.PendingDecision);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn!.Phase);
    }

    [Fact]
    public void FailedFinalizeDice_Should_NotChangeState_WhenActorDoesNotMatchCurrentPlayer()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        var wrongActor = gameState.Players.First(player => player.PlayerId != currentPlayer.PlayerId);
        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));
        var pendingDecisionBeforeFailure = gameState.PendingDecision;
        var versionBeforeFailure = gameState.Version;
        var eventCountBeforeFailure = gameState.EventLog.Count;

        var failedFinalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(wrongActor.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.False(failedFinalizeResult.Success);
        Assert.Equal("Actor does not match the current player.", failedFinalizeResult.Error);
        Assert.Same(gameState, failedFinalizeResult.GameState);
        Assert.Empty(failedFinalizeResult.NewEvents);
        Assert.Null(failedFinalizeResult.PendingDecision);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventCountBeforeFailure, gameState.EventLog.Count);
        Assert.Same(pendingDecisionBeforeFailure, gameState.PendingDecision);
        Assert.False(gameState.CurrentTurn!.DiceResolved);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn.Phase);
        Assert.Contains(gameState.EventLog, e => e is DiceRolledEvent rolled &&
                                              rolled.PlayerId == currentPlayer.PlayerId);
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
