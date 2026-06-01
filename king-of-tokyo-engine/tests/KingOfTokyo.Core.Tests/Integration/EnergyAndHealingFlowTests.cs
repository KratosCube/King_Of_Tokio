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

public sealed class EnergyAndHealingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GrantEnergy_AndHeal_WhenOutsideTokyo()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.TakeDamage(4);

        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.Heart,
            DieFace.Energy, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        Assert.True(result.Success);
        Assert.Equal(9, currentPlayer.Health);
        Assert.Equal(2, currentPlayer.Energy);
        Assert.Contains(result.NewEvents, e => e is EnergyGainedEvent);
        Assert.Contains(result.NewEvents, e => e is PlayerHealedEvent);
    }

    [Fact]
    public void FinalizeDice_Should_GrantEnergy_ButNotHeal_WhenPlayerIsInTokyo()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.TakeDamage(4);
        currentPlayer.SetTokyoSlot(TokyoSlot.City);

        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.Heart,
            DieFace.Energy, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(0));

        Assert.True(result.Success);
        Assert.Equal(6, currentPlayer.Health);
        Assert.Equal(2, currentPlayer.Energy);
        Assert.Contains(result.NewEvents, e => e is EnergyGainedEvent);
        Assert.DoesNotContain(result.NewEvents, e => e is PlayerHealedEvent);
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