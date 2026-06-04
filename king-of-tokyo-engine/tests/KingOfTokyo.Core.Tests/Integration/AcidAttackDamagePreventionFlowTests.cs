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

public sealed class AcidAttackDamagePreventionFlowTests
{
    [Fact]
    public void FinalizeDice_Should_ApplyArmorPlatingAfterAcidAttackDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating", 4));
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
            Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(9, defender.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == defender.PlayerId &&
                                             damage.Amount == 1 &&
                                             damage.DamageKind == DamageKind.Attack);
    }

    [Fact]
    public void FinalizeDice_Should_ApplyCamouflageAfterAcidAttackDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
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
        Assert.Equal(9, defender.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == defender.PlayerId &&
                                             damage.Amount == 1 &&
                                             damage.DamageKind == DamageKind.Attack);
    }

    [Fact]
    public void FinalizeDice_Should_ApplyArmorPlatingBeforeCamouflage_WhenBothPreventAcidAttackDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating", 4));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
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
                DieFace.Heart
            });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(10, defender.Health);
        Assert.Null(result.PendingDecision);
        Assert.False(gameState.CurrentTurn!.Flags.DealtDamage);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.SourcePlayerId == attacker.PlayerId &&
                                                    damage.TargetPlayerId == defender.PlayerId);
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
