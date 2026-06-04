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

public sealed class FireBreathingNeighborTargetingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_AddFireBreathingDamageToOnlyOpponent_WhenTwoPlayerTokyoAttackerRollsAttack()
    {
        var gameState = CreateGameState(2);
        var attacker = gameState.GetCurrentPlayer();
        var target = gameState.GetPlayerById(1);
        PutInCity(gameState, attacker);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, target.Health);
        Assert.Single(result.NewEvents.OfType<DamageDealtEvent>());
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.TargetPlayerId == target.PlayerId &&
                                             damage.Amount == 2 &&
                                             damage.DamageKind == DamageKind.Attack);
    }

    [Fact]
    public void FinalizeDice_Should_AddFireBreathingDamageToBothOutsideNeighbors_WhenThreePlayerTokyoAttackerRollsAttack()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        PutInCity(gameState, attacker);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, targetA.Health);
        Assert.Equal(8, targetB.Health);
        Assert.Equal(2, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == attacker.PlayerId &&
            damage.Amount == 2 &&
            damage.DamageKind == DamageKind.Attack));
    }

    [Fact]
    public void FinalizeDice_Should_AddFireBreathingDamageOnlyToNeighborTargets_WhenFourPlayerTokyoAttackerRollsAttack()
    {
        var gameState = CreateGameState(4);
        var attacker = gameState.GetCurrentPlayer();
        var clockwiseNeighbor = gameState.GetPlayerById(1);
        var nonNeighbor = gameState.GetPlayerById(2);
        var counterClockwiseNeighbor = gameState.GetPlayerById(3);
        PutInCity(gameState, attacker);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, clockwiseNeighbor.Health);
        Assert.Equal(9, nonNeighbor.Health);
        Assert.Equal(8, counterClockwiseNeighbor.Health);
        AssertDamage(result, attacker.PlayerId, clockwiseNeighbor.PlayerId, 2);
        AssertDamage(result, attacker.PlayerId, nonNeighbor.PlayerId, 1);
        AssertDamage(result, attacker.PlayerId, counterClockwiseNeighbor.PlayerId, 2);
    }

    [Fact]
    public void FinalizeDice_Should_NotDamageBayNeighbor_WhenTokyoCityAttackerFireBreathingTargetsOutsideMonstersInFivePlayerGame()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var bayNeighbor = gameState.GetPlayerById(1);
        var outsideNonNeighborA = gameState.GetPlayerById(2);
        var outsideNonNeighborB = gameState.GetPlayerById(3);
        var outsideNeighbor = gameState.GetPlayerById(4);
        PutInCity(gameState, attacker);
        PutInBay(gameState, bayNeighbor);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(10, bayNeighbor.Health);
        Assert.Equal(9, outsideNonNeighborA.Health);
        Assert.Equal(9, outsideNonNeighborB.Health);
        Assert.Equal(8, outsideNeighbor.Health);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.TargetPlayerId == bayNeighbor.PlayerId);
        AssertDamage(result, attacker.PlayerId, outsideNonNeighborA.PlayerId, 1);
        AssertDamage(result, attacker.PlayerId, outsideNonNeighborB.PlayerId, 1);
        AssertDamage(result, attacker.PlayerId, outsideNeighbor.PlayerId, 2);
    }

    [Fact]
    public void FinalizeDice_Should_AddFireBreathingDamageOnlyToNeighborTokyoTarget_WhenSixPlayerOutsideAttackerTargetsCityAndBay()
    {
        var gameState = CreateGameState(6);
        var attacker = gameState.GetCurrentPlayer();
        var cityNeighborTarget = gameState.GetPlayerById(1);
        var bayNonNeighborTarget = gameState.GetPlayerById(3);
        var outsideNonTargetA = gameState.GetPlayerById(2);
        var outsideNeighborNonTarget = gameState.GetPlayerById(5);
        PutInCity(gameState, cityNeighborTarget);
        PutInBay(gameState, bayNonNeighborTarget);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, cityNeighborTarget.Health);
        Assert.Equal(9, bayNonNeighborTarget.Health);
        Assert.Equal(10, outsideNonTargetA.Health);
        Assert.Equal(10, outsideNeighborNonTarget.Health);
        AssertDamage(result, attacker.PlayerId, cityNeighborTarget.PlayerId, 2);
        AssertDamage(result, attacker.PlayerId, bayNonNeighborTarget.PlayerId, 1);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.TargetPlayerId == outsideNeighborNonTarget.PlayerId);
    }

    [Fact]
    public void FinalizeDice_Should_AddFireBreathingDamageToBothCityAndBayTargets_WhenBothAreAttackerNeighborsInSixPlayerGame()
    {
        var gameState = CreateGameState(6);
        var attacker = gameState.GetCurrentPlayer();
        var cityNeighborTarget = gameState.GetPlayerById(1);
        var bayNeighborTarget = gameState.GetPlayerById(5);
        var outsideNonTarget = gameState.GetPlayerById(3);
        PutInCity(gameState, cityNeighborTarget);
        PutInBay(gameState, bayNeighborTarget);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        var engine = CreateEngineWithOneAttack();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(8, cityNeighborTarget.Health);
        Assert.Equal(8, bayNeighborTarget.Health);
        Assert.Equal(10, outsideNonTarget.Health);
        AssertDamage(result, attacker.PlayerId, cityNeighborTarget.PlayerId, 2);
        AssertDamage(result, attacker.PlayerId, bayNeighborTarget.PlayerId, 2);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngineWithOneAttack()
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Three,
                DieFace.Energy
            })));
    }

    private static void PutInCity(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);
    }

    private static void PutInBay(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetBayOccupant(player.PlayerId);
    }

    private static void AssertDamage(CommandResult result, int sourcePlayerId, int targetPlayerId, int amount)
    {
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == sourcePlayerId &&
                                             damage.TargetPlayerId == targetPlayerId &&
                                             damage.Amount == amount &&
                                             damage.DamageKind == DamageKind.Attack);
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
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
