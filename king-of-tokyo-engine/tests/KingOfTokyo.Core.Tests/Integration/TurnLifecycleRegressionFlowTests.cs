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

public sealed class TurnLifecycleRegressionFlowTests
{
    [Fact]
    public void BeginTurn_Should_AwardTokyoStartVictoryPoints_WhenCurrentPlayerStartsInCity()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var tokyoPlayer = gameState.GetCurrentPlayer();
        tokyoPlayer.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoPlayer.PlayerId);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(tokyoPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn!.Phase);
        Assert.True(gameState.CurrentTurn.Flags.StartedTurnInTokyo);
        Assert.True(gameState.CurrentTurn.Flags.ScoredVictoryPoints);
        Assert.Equal(2, tokyoPlayer.VictoryPoints);
        Assert.Contains(beginTurnResult.NewEvents, e => e is TurnStartedEvent started &&
                                                     started.PlayerId == tokyoPlayer.PlayerId);
        Assert.Contains(beginTurnResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                     gained.PlayerId == tokyoPlayer.PlayerId &&
                                                     gained.Amount == 2 &&
                                                     gained.Reason == "Started turn in Tokyo.");
    }

    [Fact]
    public void FullRound_Should_AwardTokyoStartPointsAndAdvanceToNextPlayer()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var tokyoPlayer = gameState.GetCurrentPlayer();
        tokyoPlayer.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoPlayer.PlayerId);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(tokyoPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(tokyoPlayer.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(tokyoPlayer.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(tokyoPlayer.PlayerId));
        var advanceResult = engine.Execute(gameState, new AdvanceToNextPlayerCommand());
        var nextPlayer = gameState.GetCurrentPlayer();
        var beginNextTurnResult = engine.Execute(gameState, new BeginTurnCommand(nextPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.True(advanceResult.Success, advanceResult.Error);
        Assert.True(beginNextTurnResult.Success, beginNextTurnResult.Error);

        Assert.NotEqual(tokyoPlayer.PlayerId, nextPlayer.PlayerId);
        Assert.Equal(TokyoSlot.City, tokyoPlayer.TokyoSlot);
        Assert.Equal(tokyoPlayer.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(2, tokyoPlayer.VictoryPoints);
        Assert.Equal(2, tokyoPlayer.Energy);
        Assert.Equal(nextPlayer.PlayerId, gameState.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn.Phase);
        Assert.False(gameState.CurrentTurn.Flags.StartedTurnInTokyo);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Null(gameState.WinnerInfo);
    }

    [Fact]
    public void FailedCommand_Should_NotChangeVersionOrAppendEvents_WhenActorDoesNotMatchCurrentPlayer()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        var wrongActor = gameState.Players.First(player => player.PlayerId != currentPlayer.PlayerId);
        var versionBeforeFailure = gameState.Version;
        var eventCountBeforeFailure = gameState.EventLog.Count;

        var failedBeginTurnResult = engine.Execute(gameState, new BeginTurnCommand(wrongActor.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.False(failedBeginTurnResult.Success);
        Assert.Equal("Actor does not match the current player.", failedBeginTurnResult.Error);
        Assert.Same(gameState, failedBeginTurnResult.GameState);
        Assert.Empty(failedBeginTurnResult.NewEvents);
        Assert.Null(failedBeginTurnResult.PendingDecision);
        Assert.Equal(versionBeforeFailure, gameState.Version);
        Assert.Equal(eventCountBeforeFailure, gameState.EventLog.Count);
        Assert.Null(gameState.CurrentTurn);
        Assert.Null(gameState.PendingDecision);
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
