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

public sealed class HealingRayHeartAllocationFlowTests
{
    [Fact]
    public void ReservedHeart_CanHealOtherMonsterBeforeOwnerUsesRemainingHeart()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        healer.TakeDamage(2);
        target.TakeDamage(2);
        healer.AddKeepCard(new MarketCardState(KnownCardIds.HealingRay, "Healing Ray", "Heal others.", 4, MarketCardType.Keep));
        var game = new GameState(new[] { healer, target }, new GameOptions(2));
        var engine = new GameEngine(diceRollService: new DiceRollService(new Faces(
            DieFace.Heart, DieFace.Heart, DieFace.One, DieFace.Two, DieFace.Three, DieFace.Energy)));
        engine.Execute(game, new InitializeGameCommand());
        engine.Execute(game, new BeginTurnCommand(0));
        engine.Execute(game, new RollDiceCommand(0));

        var finalized = engine.Execute(game, new FinalizeDiceCommand(0, heartsReservedForHealingRay: 1));
        Assert.True(finalized.Success, finalized.Error);
        Assert.Equal(9, healer.Health);
        Assert.Equal(1, game.CurrentTurn!.HeartsUsedElsewhere);
        var healed = engine.Execute(game, new ActivateHealingRayCommand(1, 1, 0));
        Assert.True(healed.Success, healed.Error);
        Assert.Equal(9, target.Health);
    }

    [Fact]
    public void HeartUsedToHealOwner_CannotAlsoHealAnotherMonster()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        var third = new PlayerState(2, "Third");
        healer.TakeDamage(1);
        target.TakeDamage(2);
        healer.AddKeepCard(new MarketCardState(KnownCardIds.HealingRay, "Healing Ray", "Heal others.", 4, MarketCardType.Keep));
        var game = new GameState(new[] { healer, target, third }, new GameOptions(3));
        var engine = new GameEngine(diceRollService: new DiceRollService(new Faces(
            DieFace.Heart, DieFace.Heart, DieFace.One, DieFace.Two, DieFace.Three, DieFace.Energy)));
        engine.Execute(game, new InitializeGameCommand());
        engine.Execute(game, new BeginTurnCommand(0));
        engine.Execute(game, new RollDiceCommand(0));
        var finalized = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.True(finalized.Success, finalized.Error);
        Assert.Equal(10, healer.Health);
        Assert.Equal(1, game.CurrentTurn!.HeartsUsedElsewhere);

        var tooMany = engine.Execute(game, new ActivateHealingRayCommand(1, 2, 0));
        Assert.False(tooMany.Success);
        Assert.Equal(8, target.Health);
        var allowed = engine.Execute(game, new ActivateHealingRayCommand(1, 1, 0));
        Assert.True(allowed.Success, allowed.Error);
        Assert.Equal(9, target.Health);
    }

    private sealed class Faces : IRandomSource
    {
        private readonly Queue<DieFace> _faces;
        public Faces(params DieFace[] faces) => _faces = new Queue<DieFace>(faces);
        public DieFace RollDieFace() => _faces.Dequeue();
    }
}
