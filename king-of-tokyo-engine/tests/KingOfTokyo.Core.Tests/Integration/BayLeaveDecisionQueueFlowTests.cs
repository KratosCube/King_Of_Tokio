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

public sealed class BayLeaveDecisionQueueFlowTests
{
    [Fact]
    public void ChooseLeaveTokyo_Should_ProcessCityAndBayLeaveDecisionsSequentially()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.NotNull(finalizeResult.PendingDecision);
        var firstPayload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision!.Payload);
        Assert.Equal(cityOccupant.PlayerId, firstPayload.DefenderPlayerId);

        var firstDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, cityOccupant.PlayerId));

        Assert.True(firstDecisionResult.Success, firstDecisionResult.Error);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(TokyoSlot.None, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.NotNull(firstDecisionResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, firstDecisionResult.PendingDecision!.DecisionType);
        var secondPayload = Assert.IsType<LeaveTokyoDecisionData>(firstDecisionResult.PendingDecision.Payload);
        Assert.Equal(bayOccupant.PlayerId, secondPayload.DefenderPlayerId);
        Assert.Contains(firstDecisionResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                         left.PlayerId == cityOccupant.PlayerId &&
                                                         left.Slot == TokyoSlot.City);
        Assert.Contains(firstDecisionResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                         entered.PlayerId == attacker.PlayerId &&
                                                         entered.Slot == TokyoSlot.City);

        var secondDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, bayOccupant.PlayerId));

        Assert.True(secondDecisionResult.Success, secondDecisionResult.Error);
        Assert.Null(secondDecisionResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Contains(secondDecisionResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                          left.PlayerId == bayOccupant.PlayerId &&
                                                          left.Slot == TokyoSlot.Bay);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_KeepBayOccupied_WhenCityDefenderLeavesAndBayDefenderStays()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.NotNull(finalizeResult.PendingDecision);
        var firstPayload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision!.Payload);
        Assert.Equal(cityOccupant.PlayerId, firstPayload.DefenderPlayerId);

        var firstDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, cityOccupant.PlayerId));

        Assert.True(firstDecisionResult.Success, firstDecisionResult.Error);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(TokyoSlot.None, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.NotNull(firstDecisionResult.PendingDecision);
        var secondPayload = Assert.IsType<LeaveTokyoDecisionData>(firstDecisionResult.PendingDecision!.Payload);
        Assert.Equal(bayOccupant.PlayerId, secondPayload.DefenderPlayerId);

        var secondDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, bayOccupant.PlayerId));

        Assert.True(secondDecisionResult.Success, secondDecisionResult.Error);
        Assert.Null(secondDecisionResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.DoesNotContain(secondDecisionResult.NewEvents, e => e is TokyoEnteredEvent);
        Assert.DoesNotContain(secondDecisionResult.NewEvents, e => e is TokyoLeftEvent);
        Assert.DoesNotContain(secondDecisionResult.NewEvents, e => e is VictoryPointsGainedEvent);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_NotMoveAttackerUntilBaySlotOpens_WhenCityDefenderStaysAndBayDefenderLeaves()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.NotNull(finalizeResult.PendingDecision);
        var firstPayload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision!.Payload);
        Assert.Equal(cityOccupant.PlayerId, firstPayload.DefenderPlayerId);

        var firstDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, cityOccupant.PlayerId));

        Assert.True(firstDecisionResult.Success, firstDecisionResult.Error);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.NotNull(firstDecisionResult.PendingDecision);
        var secondPayload = Assert.IsType<LeaveTokyoDecisionData>(firstDecisionResult.PendingDecision!.Payload);
        Assert.Equal(bayOccupant.PlayerId, secondPayload.DefenderPlayerId);
        Assert.DoesNotContain(firstDecisionResult.NewEvents, e => e is TokyoEnteredEvent);

        var secondDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, bayOccupant.PlayerId));

        Assert.True(secondDecisionResult.Success, secondDecisionResult.Error);
        Assert.Null(secondDecisionResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, attacker.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Contains(secondDecisionResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                          left.PlayerId == bayOccupant.PlayerId &&
                                                          left.Slot == TokyoSlot.Bay);
        Assert.Contains(secondDecisionResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                          entered.PlayerId == attacker.PlayerId &&
                                                          entered.Slot == TokyoSlot.Bay);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_LeaveAttackerOutside_WhenCityAndBayDefendersBothStay()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.NotNull(finalizeResult.PendingDecision);

        var firstDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, cityOccupant.PlayerId));

        Assert.True(firstDecisionResult.Success, firstDecisionResult.Error);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.NotNull(firstDecisionResult.PendingDecision);
        Assert.DoesNotContain(firstDecisionResult.NewEvents, e => e is TokyoEnteredEvent);

        var secondDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, bayOccupant.PlayerId));

        Assert.True(secondDecisionResult.Success, secondDecisionResult.Error);
        Assert.Null(secondDecisionResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.Equal(0, attacker.VictoryPoints);
        Assert.DoesNotContain(secondDecisionResult.NewEvents, e => e is TokyoEnteredEvent);
        Assert.DoesNotContain(secondDecisionResult.NewEvents, e => e is VictoryPointsGainedEvent);
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
