using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class BayFlowTests
{
    [Fact]
    public void FinalizeDice_Should_DisableBay_WhenAlivePlayersDropBelowFive()
    {
        var gameState = CreateGameState(5);

        var attacker = gameState.GetPlayerById(0);
        var cityDefender = gameState.GetPlayerById(1);
        var bayDefender = gameState.GetPlayerById(2);

        cityDefender.SetTokyoSlot(TokyoSlot.City);
        bayDefender.SetTokyoSlot(TokyoSlot.Bay);

        cityDefender.TakeDamage(9);
        bayDefender.TakeDamage(9);

        gameState.Tokyo.SetCityOccupant(cityDefender.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayDefender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.False(cityDefender.IsAlive);
        Assert.False(bayDefender.IsAlive);
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Null(gameState.Tokyo.BayOccupantId);
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