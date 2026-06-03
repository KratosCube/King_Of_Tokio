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

public sealed class NoBayAttackFlowTests
{
    [Fact]
    public void ChooseLeaveTokyo_Should_NotMoveAttackerIntoBay_WhenFourPlayerCityDefenderStays()
    {
        var gameState = CreateGameState(4);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);

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
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, finalizeResult.PendingDecision!.DecisionType);
        Assert.Equal(9, cityOccupant.Health);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision.Payload);
        Assert.Equal(cityOccupant.PlayerId, payload.DefenderPlayerId);

        var stayResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, cityOccupant.PlayerId));

        Assert.True(stayResult.Success, stayResult.Error);
        Assert.Null(stayResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(0, attacker.VictoryPoints);
        Assert.DoesNotContain(stayResult.NewEvents, e => e is TokyoEnteredEvent);
        Assert.DoesNotContain(stayResult.NewEvents, e => e is VictoryPointsGainedEvent);
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
