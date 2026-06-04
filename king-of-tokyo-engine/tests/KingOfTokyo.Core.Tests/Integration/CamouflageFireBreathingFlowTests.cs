using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
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

public sealed class CamouflageFireBreathingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_ApplyCamouflageAgainstFireBreathingEnhancedAttackDamage()
    {
        var gameState = CreateGameState(4);
        var attacker = gameState.GetCurrentPlayer();
        var camouflagedNeighbor = gameState.GetPlayerById(1);
        var nonNeighbor = gameState.GetPlayerById(2);
        var uncamouflagedNeighbor = gameState.GetPlayerById(3);
        PutInCity(gameState, attacker);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        camouflagedNeighbor.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
        var engine = CreateEngine(
            new[]
            {
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Three,
                DieFace.Energy
            },
            new[]
            {
                DieFace.Heart,
                DieFace.Attack
            });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(9, camouflagedNeighbor.Health);
        Assert.Equal(9, nonNeighbor.Health);
        Assert.Equal(8, uncamouflagedNeighbor.Health);
        AssertDamage(result, attacker.PlayerId, camouflagedNeighbor.PlayerId, 1);
        AssertDamage(result, attacker.PlayerId, nonNeighbor.PlayerId, 1);
        AssertDamage(result, attacker.PlayerId, uncamouflagedNeighbor.PlayerId, 2);
    }

    [Fact]
    public void FinalizeDice_Should_AllowCamouflageToPreventAllFireBreathingEnhancedDamageAgainstBayTarget()
    {
        var gameState = CreateGameState(6);
        var attacker = gameState.GetCurrentPlayer();
        var cityNeighborTarget = gameState.GetPlayerById(1);
        var outsideNonTarget = gameState.GetPlayerById(2);
        var bayNonNeighborTarget = gameState.GetPlayerById(3);
        var outsideNeighborNonTarget = gameState.GetPlayerById(5);
        PutInCity(gameState, cityNeighborTarget);
        PutInBay(gameState, bayNonNeighborTarget);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.FireBreathing, "Fire Breathing", 4));
        cityNeighborTarget.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
        var engine = CreateEngine(
            new[]
            {
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Three,
                DieFace.Energy
            },
            new[]
            {
                DieFace.Heart,
                DieFace.Heart
            });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(10, cityNeighborTarget.Health);
        Assert.Equal(9, bayNonNeighborTarget.Health);
        Assert.Equal(10, outsideNonTarget.Health);
        Assert.Equal(10, outsideNeighborNonTarget.Health);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.TargetPlayerId == cityNeighborTarget.PlayerId);
        AssertDamage(result, attacker.PlayerId, bayNonNeighborTarget.PlayerId, 1);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(
        IReadOnlyCollection<DieFace> diceFaces,
        IReadOnlyCollection<DieFace> camouflageFaces)
    {
        var keepCardRulesService = new KeepCardRulesService();
        var damagePreventionService = new DamagePreventionService(
            keepCardRulesService,
            new SequenceRandomSource(camouflageFaces));
        var damageApplier = new DamageApplier(
            keepCardRulesService,
            damagePreventionService);
        var finalizeDiceService = new FinalizeDiceService(
            attackResolver: new AttackResolver(keepCardRulesService),
            damageApplier: damageApplier,
            keepCardRulesService: keepCardRulesService);

        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(diceFaces)),
            finalizeDiceService: finalizeDiceService,
            keepCardRulesService: keepCardRulesService);
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
