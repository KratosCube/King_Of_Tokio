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

public sealed class FullTurnFlowTests
{
    [Fact]
    public void InitializeGame_Should_SetStatusToRunning()
    {
        var gameState = CreateGameState(4);
        var engine = new GameEngine();

        var result = engine.Execute(gameState, new InitializeGameCommand());

        Assert.True(result.Success);
        Assert.Equal(GameStatus.Running, gameState.Status);
    }

    [Fact]
    public void BeginTurn_Should_CreateCurrentTurn_AndMoveToRollingPhase()
    {
        var gameState = CreateGameState(4);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        var result = engine.Execute(gameState, new BeginTurnCommand(0));

        Assert.True(result.Success);
        Assert.NotNull(gameState.CurrentTurn);
        Assert.Equal(0, gameState.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn.Phase);
        Assert.Contains(result.NewEvents, e => e is TurnStartedEvent);
    }

    [Fact]
    public void BeginTurn_Should_GrantTwoVictoryPoints_WhenCurrentPlayerStartsInTokyo()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.SetTokyoSlot(TokyoSlot.City);

        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        var result = engine.Execute(gameState, new BeginTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(2, currentPlayer.VictoryPoints);
        Assert.True(gameState.CurrentTurn!.Flags.StartedTurnInTokyo);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent);
    }

    [Fact]
    public void EndTurn_Should_FinishTurn_WhenNoWinnerExists()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var result = engine.Execute(gameState, new EndTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Equal(TurnPhase.Finished, gameState.CurrentTurn!.Phase);
    }

    [Fact]
    public void EndTurn_Should_ApplyPoisonDamage()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.Status.AddPoisonTokens(2);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var result = engine.Execute(gameState, new EndTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(8, currentPlayer.Health);

        var damageEvent = Assert.Single(result.NewEvents.OfType<DamageDealtEvent>());
        Assert.Equal(currentPlayer.PlayerId, damageEvent.SourcePlayerId);
        Assert.Equal(currentPlayer.PlayerId, damageEvent.TargetPlayerId);
        Assert.Equal(2, damageEvent.Amount);
        Assert.Equal(DamageKind.StatusEffect, damageEvent.DamageKind);
    }

    [Fact]
    public void EndTurn_Should_EndGame_WhenPoisonEliminatesCurrentPlayerAndOneMonsterRemains()
    {
        var gameState = CreateGameState(2);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.TakeDamage(9);
        currentPlayer.Status.AddPoisonTokens(1);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var result = engine.Execute(gameState, new EndTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.Equal(1, gameState.WinnerInfo!.WinnerPlayerId);
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                             eliminated.EliminatedPlayerId == currentPlayer.PlayerId);
        Assert.Contains(result.NewEvents, e => e is GameEndedEvent ended &&
                                             ended.WinnerPlayerId == 1);
    }

    [Fact]
    public void AdvanceToNextPlayer_Should_MoveCurrentPlayerIndex_AfterFinishedTurn()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));
        engine.Execute(gameState, new EndTurnCommand(0));

        var result = engine.Execute(gameState, new AdvanceToNextPlayerCommand());

        Assert.True(result.Success);
        Assert.Equal(1, gameState.GetCurrentPlayer().PlayerId);
    }

    [Fact]
    public void EndTurn_Should_EndGame_WhenCurrentPlayerHasTwentyPoints()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.GainVictoryPoints(19);

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var result = engine.Execute(gameState, new EndTurnCommand(0));

        Assert.True(result.Success);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.Equal(currentPlayer.PlayerId, gameState.WinnerInfo!.WinnerPlayerId);
        Assert.Contains(result.NewEvents, e => e is GameEndedEvent ended &&
                                               ended.WinnerPlayerId == currentPlayer.PlayerId);
    }

    [Fact]
    public void AdvanceToNextPlayer_Should_SkipDeadPlayers()
    {
        var gameState = CreateGameState(4);
        gameState.GetPlayerById(1).TakeDamage(10);

        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));
        engine.Execute(gameState, new EndTurnCommand(0));

        var result = engine.Execute(gameState, new AdvanceToNextPlayerCommand());

        Assert.True(result.Success);
        Assert.Equal(2, gameState.GetCurrentPlayer().PlayerId);
    }

    [Fact]
    public void AdvanceToNextPlayer_Should_Fail_WhenTurnIsNotFinished()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Two, DieFace.Two, DieFace.Heart,
            DieFace.Heart, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));

        var result = engine.Execute(gameState, new AdvanceToNextPlayerCommand());

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = CreatePlayers(playerCount);
        var options = new GameOptions(playerCount);
        return new GameState(players, options);
    }

    private static IReadOnlyList<PlayerState> CreatePlayers(int count)
    {
        var players = new List<PlayerState>();

        for (var i = 0; i < count; i++)
        {
            players.Add(new PlayerState(i, $"Monster {i + 1}"));
        }

        return players;
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
