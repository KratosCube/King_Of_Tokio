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

public sealed class BayOccupantEliminationCleanupFlowTests
{
    [Fact]
    public void FinalizeDice_Should_ClearAndDisableBay_WhenBayOccupantIsEliminatedAndAliveCountDropsBelowFive()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        bayOccupant.TakeDamage(9);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
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

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(bayOccupant.IsAlive);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, result.PendingDecision!.DecisionType);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(cityOccupant.PlayerId, payload.DefenderPlayerId);
        Assert.Equal(attacker.PlayerId, payload.AttackerPlayerId);
        Assert.Equal(1, payload.DamageTaken);
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                             eliminated.EliminatedPlayerId == bayOccupant.PlayerId &&
                                             eliminated.EliminatedByPlayerId == attacker.PlayerId);
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
