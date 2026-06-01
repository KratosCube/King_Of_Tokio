using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class AttackFlowTests
{
    [Fact]
    public void FinalizeDice_Should_DamageTokyoOccupant_WhenAttackerIsOutsideTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Attack, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(1, result.PendingDecision!.PlayerId);

        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.SourcePlayerId == attacker.PlayerId &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 2);
    }

    [Fact]
    public void FinalizeDice_Should_ApplyCamouflagePrevention_WhenTokyoOccupantTakesAttackDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.Camouflage,
            "Camouflage",
            "When you take damage, roll a die for each damage. Each heart prevents 1 damage.",
            3,
            MarketCardType.Keep));
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            diceFaces: new[]
            {
                DieFace.Attack, DieFace.Attack, DieFace.Attack,
                DieFace.One, DieFace.Two, DieFace.Energy
            },
            camouflageFaces: new[]
            {
                DieFace.Heart, DieFace.Attack, DieFace.Heart
            });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(9, defender.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, result.PendingDecision!.DecisionType);

        var leavePayload = Assert.IsType<LeaveTokyoDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(1, leavePayload.DamageTaken);

        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.SourcePlayerId == attacker.PlayerId &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 1);
    }

    [Fact]
    public void FinalizeDice_Should_DamageAllOutsideTokyo_WhenAttackerIsInTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defenderA = gameState.GetPlayerById(1);
        var defenderB = gameState.GetPlayerById(2);

        attacker.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(attacker.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(9, defenderA.Health);
        Assert.Equal(9, defenderB.Health);
        Assert.Equal(10, attacker.Health);

        var damageEvents = result.NewEvents.OfType<DamageDealtEvent>().ToArray();
        Assert.Equal(2, damageEvents.Length);
        Assert.Null(result.PendingDecision);
    }

    [Fact]
    public void FinalizeDice_Should_EliminateTokyoDefender_AndAttackerShouldEnterTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);

        defender.SetTokyoSlot(TokyoSlot.City);
        defender.TakeDamage(9);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.False(defender.IsAlive);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(1, attacker.VictoryPoints);

        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                               eliminated.EliminatedPlayerId == defender.PlayerId);

        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                               entered.PlayerId == attacker.PlayerId);
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

    private static GameEngine CreateEngine(DieFace[] diceFaces, DieFace[] camouflageFaces)
    {
        var damagePreventionService = new DamagePreventionService(
            randomSource: new SequenceRandomSource(camouflageFaces));
        var damageApplier = new DamageApplier(damagePreventionService: damagePreventionService);
        var finalizeDiceService = new FinalizeDiceService(damageApplier: damageApplier);

        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(diceFaces)),
            finalizeDiceService: finalizeDiceService);
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
