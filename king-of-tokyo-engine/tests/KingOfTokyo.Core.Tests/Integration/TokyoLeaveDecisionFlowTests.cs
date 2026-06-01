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

public sealed class TokyoLeaveDecisionFlowTests
{
    [Fact]
    public void FinalizeDice_Should_CreatePendingLeaveTokyoDecision_ForTokyoDefender()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.NotNull(result.PendingDecision);
        Assert.NotNull(gameState.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, result.PendingDecision!.DecisionType);
        Assert.Equal(defender.PlayerId, result.PendingDecision.PlayerId);
        Assert.Equal(TurnPhase.DiceResolved, gameState.CurrentTurn!.Phase);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_MoveDefenderOut_AndAttackerIntoTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);

        Assert.Contains(result.NewEvents, e => e is TokyoLeftEvent left &&
                                               left.PlayerId == defender.PlayerId);

        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                               entered.PlayerId == attacker.PlayerId);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_KeepDefenderInTokyo_WhenPlayerStays()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, defender.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(TokyoSlot.City, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(defender.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(0, attacker.VictoryPoints);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Empty(result.NewEvents);
    }

    [Fact]
    public void EndTurn_Should_Fail_WhileTokyoLeaveDecisionIsPending()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(attacker.PlayerId));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
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