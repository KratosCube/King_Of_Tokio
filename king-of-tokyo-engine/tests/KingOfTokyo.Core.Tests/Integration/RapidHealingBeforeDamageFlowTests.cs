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

public sealed class RapidHealingBeforeDamageFlowTests
{
    [Fact]
    public void Defender_CanHealBeforeLethalAttackAndOnlyThatDefenderCanResumeDamage()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = CreateWoundedHealer(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);

        var invalid = engine.Execute(game, new FinalizeDiceCommand(0, heartsReservedForHealingRay: -1));
        Assert.False(invalid.Success);
        Assert.Equal(1, defender.Health);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Empty(window.NewEvents);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.Equal(defender.PlayerId, window.PendingDecision.PlayerId);
        Assert.Equal(1, defender.Health);

        var wrongActor = engine.Execute(game, new ContinueAfterRapidHealingCommand(attacker.PlayerId));
        Assert.False(wrongActor.Success);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);

        var healed = engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId));
        Assert.True(healed.Success, healed.Error);
        Assert.Equal(2, defender.Health);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);

        var resumed = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resumed.Success, resumed.Error);
        Assert.Equal(1, defender.Health);
        Assert.True(defender.IsAlive);
        Assert.Equal(DecisionType.LeaveTokyo, resumed.PendingDecision!.DecisionType);
        Assert.Contains(resumed.NewEvents, gameEvent => gameEvent is DamageDealtEvent damage &&
            damage.TargetPlayerId == defender.PlayerId && damage.Amount == 1);
    }

    [Fact]
    public void MultipleDefenders_RespondInTurnBeforeAnyAttackDamageIsApplied()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.SetTokyoSlot(TokyoSlot.City);
        var first = CreateWoundedHealer(1, health: 1);
        var second = CreateWoundedHealer(2, health: 1);
        var game = new GameState(new[] { attacker, first, second }, new GameOptions(3));
        game.Tokyo.SetCityOccupant(attacker.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.Equal(first.PlayerId, window.PendingDecision!.PlayerId);
        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(first.PlayerId)).Success);

        var next = engine.Execute(game, new ContinueAfterRapidHealingCommand(first.PlayerId));
        Assert.True(next.Success, next.Error);
        Assert.Equal(second.PlayerId, next.PendingDecision!.PlayerId);
        Assert.Equal(2, first.Health);
        Assert.Equal(1, second.Health);
        Assert.Empty(next.NewEvents);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(second.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, first.Health);
        Assert.False(second.IsAlive);
        Assert.Contains(resolved.NewEvents, gameEvent => gameEvent is PlayerEliminatedEvent eliminated &&
            eliminated.EliminatedPlayerId == second.PlayerId);
    }

    [Fact]
    public void Defender_CanHealBeforeLethalPoisonQuillsDamage()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.PoisonQuills, "Poison Quills", "Damage when scoring ones.", 3, MarketCardType.Keep));
        var defender = CreateWoundedHealer(1, health: 2);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Two, DieFace.Energy);
        StartAndRoll(engine, game);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId)).Success);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, defender.Health);
        Assert.True(defender.IsAlive);
        Assert.Null(resolved.PendingDecision);
        Assert.Contains(resolved.NewEvents, gameEvent => gameEvent is DamageDealtEvent damage &&
            damage.TargetPlayerId == defender.PlayerId && damage.Amount == 2);
    }

    [Fact]
    public void Attacker_CannotAlterTheRollWhileDefenderIsDeciding()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.GainEnergy(3);
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.Telepath, "Telepath", "Extra roll.", 5, MarketCardType.Keep));
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.Stretchy, "Stretchy", "Change a die.", 4, MarketCardType.Keep));
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.HerdCuller, "Herd Culler", "Change a die.", 3, MarketCardType.Keep));
        var defender = CreateWoundedHealer(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);
        Assert.True(engine.Execute(game, new FinalizeDiceCommand(0)).Success);

        var facesBefore = game.CurrentTurn!.DicePool.Dice.Select(die => die.CurrentFace).ToArray();
        Assert.False(engine.Execute(game, new ActivateTelepathCommand(0)).Success);
        Assert.False(engine.Execute(game, new ActivateStretchyCommand(0, DieFace.Three, 0)).Success);
        Assert.False(engine.Execute(game, new ActivateHerdCullerCommand(0, 0)).Success);
        Assert.Equal(facesBefore, game.CurrentTurn.DicePool.Dice.Select(die => die.CurrentFace));
        Assert.Equal(3, attacker.Energy);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);
    }

    private static PlayerState CreateWoundedHealer(int playerId, int health)
    {
        var player = new PlayerState(playerId, $"Defender {playerId}");
        player.TakeDamage(player.MaxHealth - health);
        player.GainEnergy(2);
        player.AddKeepCard(new MarketCardState(KnownCardIds.RapidHealing, "Rapid Healing", "Heal anytime.", 3, MarketCardType.Keep));
        return player;
    }

    private static GameEngine CreateEngine(params DieFace[] faces) =>
        new(diceRollService: new DiceRollService(new Faces(faces)));

    private static void StartAndRoll(GameEngine engine, GameState game)
    {
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.True(engine.Execute(game, new RollDiceCommand(0)).Success);
    }

    private sealed class Faces : IRandomSource
    {
        private readonly Queue<DieFace> _faces;
        public Faces(IEnumerable<DieFace> faces) => _faces = new Queue<DieFace>(faces);
        public DieFace RollDieFace() => _faces.Dequeue();
    }
}
