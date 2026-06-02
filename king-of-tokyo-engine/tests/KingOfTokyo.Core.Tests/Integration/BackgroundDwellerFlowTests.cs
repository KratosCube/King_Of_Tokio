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

public sealed class BackgroundDwellerFlowTests
{
    [Fact]
    public void RollDice_Should_RerollThreesUntilNoneRemain_WhenCurrentPlayerHasBackgroundDweller()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateBackgroundDweller());
        var engine = CreateEngine(
            DieFace.Three, DieFace.Attack, DieFace.Three,
            DieFace.Heart, DieFace.Three, DieFace.Energy,
            DieFace.Three, DieFace.One, DieFace.Two, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        var result = engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain(gameState.CurrentTurn!.DicePool.Dice, die => die.CurrentFace == DieFace.Three);
        Assert.Equal(new[]
        {
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Heart,
            DieFace.Two,
            DieFace.Energy
        }, gameState.CurrentTurn.DicePool.Dice.Select(die => die.CurrentFace));
        Assert.Contains(result.NewEvents, e => e is DiceRolledEvent rolled &&
                                               rolled.PlayerId == player.PlayerId &&
                                               !rolled.Faces.Contains(DieFace.Three));
    }

    [Fact]
    public void RerollDice_Should_RerollThreesUntilNoneRemain_WhenCurrentPlayerHasBackgroundDweller()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateBackgroundDweller());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Attack,
            DieFace.Heart, DieFace.Energy, DieFace.Attack,
            DieFace.Three, DieFace.Three,
            DieFace.Three, DieFace.Heart, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new RerollDiceCommand(player.PlayerId, new[] { 0, 1 }));

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain(gameState.CurrentTurn!.DicePool.Dice, die => die.CurrentFace == DieFace.Three);
        Assert.Equal(DieFace.Attack, gameState.CurrentTurn.DicePool.Dice[0].CurrentFace);
        Assert.Equal(DieFace.Heart, gameState.CurrentTurn.DicePool.Dice[1].CurrentFace);
        Assert.Contains(result.NewEvents, e => e is DiceRolledEvent rolled &&
                                               rolled.PlayerId == player.PlayerId &&
                                               !rolled.Faces.Contains(DieFace.Three));
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateBackgroundDweller()
    {
        return new MarketCardState(
            KnownCardIds.BackgroundDweller,
            "Background Dweller",
            "Whenever you roll any 3s, reroll them until none remain.",
            4,
            MarketCardType.Keep);
    }

    private static GameEngine CreateEngine(params DieFace[] sequence)
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)));
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(params DieFace[] faces)
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
