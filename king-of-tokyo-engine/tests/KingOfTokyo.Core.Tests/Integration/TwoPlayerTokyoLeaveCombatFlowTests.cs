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

public sealed class TwoPlayerTokyoLeaveCombatFlowTests
{
    [Fact]
    public void TurnFlow_Should_HandleTokyoLeaveAndEntry_InTwoPlayerCombat()
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
        var tokyoDefender = gameState.GetCurrentPlayer();
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);
        gameState.AdvanceToNextAlivePlayer();
        var attacker = gameState.GetCurrentPlayer();

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Equal(8, tokyoDefender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, finalizeResult.PendingDecision!.DecisionType);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision.Payload);
        Assert.Equal(tokyoDefender.PlayerId, payload.DefenderPlayerId);
        Assert.Equal(attacker.PlayerId, payload.AttackerPlayerId);
        Assert.Equal(2, payload.DamageTaken);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, tokyoDefender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Null(leaveResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.None, tokyoDefender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Equal(1, attacker.Energy);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                 left.PlayerId == tokyoDefender.PlayerId &&
                                                 left.Slot == TokyoSlot.City);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                 entered.PlayerId == attacker.PlayerId &&
                                                 entered.Slot == TokyoSlot.City);
        Assert.Contains(leaveResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                 gained.PlayerId == attacker.PlayerId &&
                                                 gained.Amount == 1 &&
                                                 gained.Reason == "Entered Tokyo after defender left.");
    }

    [Fact]
    public void TurnFlow_Should_LeaveAttackerOutside_WhenTokyoDefenderStays_InTwoPlayerCombat()
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
        var tokyoDefender = gameState.GetCurrentPlayer();
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);
        gameState.AdvanceToNextAlivePlayer();
        var attacker = gameState.GetCurrentPlayer();

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));
        var stayResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, tokyoDefender.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(stayResult.Success, stayResult.Error);
        Assert.Equal(8, tokyoDefender.Health);
        Assert.Equal(TokyoSlot.City, tokyoDefender.TokyoSlot);
        Assert.Equal(tokyoDefender.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Null(stayResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(0, attacker.VictoryPoints);
        Assert.Equal(1, attacker.Energy);
        Assert.DoesNotContain(stayResult.NewEvents, e => e is TokyoEnteredEvent);
        Assert.DoesNotContain(stayResult.NewEvents, e => e is VictoryPointsGainedEvent);
    }

    [Fact]
    public void TurnFlow_Should_EndGame_WhenTokyoDefenderIsEliminated_InTwoPlayerCombat()
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
        var tokyoDefender = gameState.GetCurrentPlayer();
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        tokyoDefender.TakeDamage(8);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);
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
        Assert.False(tokyoDefender.IsAlive);
        Assert.Equal(TokyoSlot.None, tokyoDefender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(attacker.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Last monster standing.", gameState.WinnerInfo.Reason);
        Assert.Contains(finalizeResult.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                                    eliminated.EliminatedPlayerId == tokyoDefender.PlayerId &&
                                                    eliminated.EliminatedByPlayerId == attacker.PlayerId);
        Assert.Contains(finalizeResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                    entered.PlayerId == attacker.PlayerId &&
                                                    entered.Slot == TokyoSlot.City);
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                    ended.WinnerPlayerId == attacker.PlayerId &&
                                                    ended.Reason == "Last monster standing.");
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
