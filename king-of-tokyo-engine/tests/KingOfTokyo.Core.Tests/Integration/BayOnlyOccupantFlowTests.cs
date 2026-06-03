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

public sealed class BayOnlyOccupantFlowTests
{
    [Fact]
    public void ChooseLeaveTokyo_Should_MoveAttackerIntoCity_WhenOnlyBayOccupantLeaves()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var bayOccupant = gameState.GetPlayerById(1);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Equal(9, bayOccupant.Health);
        Assert.Null(gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision!.Payload);
        Assert.Equal(bayOccupant.PlayerId, payload.DefenderPlayerId);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, bayOccupant.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Null(leaveResult.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                 left.PlayerId == bayOccupant.PlayerId &&
                                                 left.Slot == TokyoSlot.Bay);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                 entered.PlayerId == attacker.PlayerId &&
                                                 entered.Slot == TokyoSlot.City);
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
