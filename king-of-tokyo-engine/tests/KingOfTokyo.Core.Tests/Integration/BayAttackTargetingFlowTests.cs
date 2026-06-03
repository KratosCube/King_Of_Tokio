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

public sealed class BayAttackTargetingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_DamageCityAndBayOccupants_WhenOutsidePlayerAttacksInBayGame()
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

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, cityOccupant.Health);
        Assert.Equal(8, bayOccupant.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, result.PendingDecision!.DecisionType);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == cityOccupant.PlayerId &&
                                             damage.Amount == 2 &&
                                             damage.DamageKind == DamageKind.Attack);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == bayOccupant.PlayerId &&
                                             damage.Amount == 2 &&
                                             damage.DamageKind == DamageKind.Attack);

        var payload = Assert.IsType<LeaveTokyoDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(attacker.PlayerId, payload.AttackerPlayerId);
        Assert.Contains(payload.DefenderPlayerId, new[] { cityOccupant.PlayerId, bayOccupant.PlayerId });
        Assert.Equal(2, payload.DamageTaken);
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
