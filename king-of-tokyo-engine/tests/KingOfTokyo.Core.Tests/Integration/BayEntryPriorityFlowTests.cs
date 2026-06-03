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

public sealed class BayEntryPriorityFlowTests
{
    [Fact]
    public void FinalizeDice_Should_EnterCityFirst_WhenBayGameTokyoIsEmpty()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
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
        Assert.True(gameState.Tokyo.BayEnabled);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.False(gameState.CurrentTurn!.Flags.DealtDamage);
        Assert.True(gameState.CurrentTurn.Flags.EnteredTokyo);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn.Phase);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent);
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                             entered.PlayerId == attacker.PlayerId &&
                                             entered.Slot == TokyoSlot.City);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                             gained.PlayerId == attacker.PlayerId &&
                                             gained.Amount == 1 &&
                                             gained.Reason == "Entered Tokyo.");
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
