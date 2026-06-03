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

public sealed class FullGameVictoryRegressionFlowTests
{
    [Fact]
    public void EndTurn_Should_EndGame_WhenCurrentPlayerSurvivesWithTwentyVictoryPoints()
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
        currentPlayer.GainVictoryPoints(18);
        currentPlayer.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(currentPlayer.PlayerId);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(currentPlayer.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.True(currentPlayer.IsAlive);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(currentPlayer.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", gameState.WinnerInfo.Reason);
        Assert.Equal(TurnPhase.Finished, gameState.CurrentTurn!.Phase);
        Assert.Contains(beginTurnResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                     gained.PlayerId == currentPlayer.PlayerId &&
                                                     gained.Amount == 2 &&
                                                     gained.Reason == "Started turn in Tokyo.");
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                   ended.WinnerPlayerId == currentPlayer.PlayerId &&
                                                   ended.Reason == "Reached 20 victory points.");
    }

    [Fact]
    public void EndTurn_Should_EndGame_WhenOnlyOneMonsterRemainsAfterCombatElimination()
    {
        var gameState = CreateGameState(2);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Energy);
        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var defender = gameState.GetCurrentPlayer();
        defender.SetTokyoSlot(TokyoSlot.City);
        defender.TakeDamage(8);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        gameState.AdvanceToNextAlivePlayer();
        var attacker = gameState.GetCurrentPlayer();

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(attacker.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.False(defender.IsAlive);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.True(attacker.IsAlive);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(attacker.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Last monster standing.", gameState.WinnerInfo.Reason);
        Assert.Equal(TurnPhase.Finished, gameState.CurrentTurn!.Phase);
        Assert.Contains(finalizeResult.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                                    eliminated.EliminatedPlayerId == defender.PlayerId &&
                                                    eliminated.EliminatedByPlayerId == attacker.PlayerId);
        Assert.Contains(finalizeResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                    entered.PlayerId == attacker.PlayerId &&
                                                    entered.Slot == TokyoSlot.City);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                   ended.WinnerPlayerId == attacker.PlayerId &&
                                                   ended.Reason == "Last monster standing.");
    }

    [Fact]
    public void EndTurn_Should_NotEndGame_WhenCurrentPlayerHasTwentyPointsButWasEliminated()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy,
            DieFace.Energy,
            DieFace.Energy);
        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.GainVictoryPoints(20);
        currentPlayer.TakeDamage(9);
        currentPlayer.Status.AddPoisonTokens(1);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(currentPlayer.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.False(currentPlayer.IsAlive);
        Assert.Equal(20, currentPlayer.VictoryPoints);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Null(gameState.WinnerInfo);
        Assert.Equal(TurnPhase.Finished, gameState.CurrentTurn!.Phase);
        Assert.Contains(endTurnResult.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                                   eliminated.EliminatedPlayerId == currentPlayer.PlayerId &&
                                                   eliminated.EliminatedByPlayerId == currentPlayer.PlayerId);
        Assert.DoesNotContain(endTurnResult.NewEvents, e => e is GameEndedEvent);
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
