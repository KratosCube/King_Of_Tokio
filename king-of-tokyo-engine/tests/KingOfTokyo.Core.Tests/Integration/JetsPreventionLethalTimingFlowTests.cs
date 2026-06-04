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

public sealed class JetsPreventionLethalTimingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_NotQueueTokyoLeaveOrHealJets_WhenTokyoDefenderIsEliminatedByLethalAttack()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.TakeDamage(8);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        var engine = CreateEngine(
            diceFaces: new[]
            {
                DieFace.Attack,
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Energy
            },
            camouflageFaces: Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(defender.IsAlive);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(result.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.DoesNotContain(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                                    healed.PlayerId == defender.PlayerId &&
                                                    healed.Reason == "Keep card: Jets.");
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                             eliminated.EliminatedPlayerId == defender.PlayerId &&
                                             eliminated.EliminatedByPlayerId == attacker.PlayerId &&
                                             eliminated.Reason == "Attack damage.");
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                             entered.PlayerId == attacker.PlayerId &&
                                             entered.Slot == TokyoSlot.City);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_HealOnlyActualPostPreventionDamageWithJets_WhenArmorPlatingPreventsLethalAttack()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.TakeDamage(8);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating", 4));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        var engine = CreateEngine(
            diceFaces: new[]
            {
                DieFace.Attack,
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Energy
            },
            camouflageFaces: Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(defender.IsAlive);
        Assert.Equal(1, defender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision!.Payload);
        Assert.Equal(1, payload.DamageTaken);
        Assert.Contains(finalizeResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                     damage.TargetPlayerId == defender.PlayerId &&
                                                     damage.Amount == 1 &&
                                                     damage.DamageKind == DamageKind.Attack);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Equal(2, defender.Health);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Contains(leaveResult.NewEvents, e => e is PlayerHealedEvent healed &&
                                                   healed.PlayerId == defender.PlayerId &&
                                                   healed.Amount == 1 &&
                                                   healed.Reason == "Keep card: Jets.");
    }

    [Fact]
    public void FinalizeDice_Should_NotQueueTokyoLeave_WhenCamouflagePreventsAllIncomingLethalAttackDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.TakeDamage(8);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage", 3));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        var engine = CreateEngine(
            diceFaces: new[]
            {
                DieFace.Attack,
                DieFace.Attack,
                DieFace.Heart,
                DieFace.One,
                DieFace.Two,
                DieFace.Energy
            },
            camouflageFaces: new[]
            {
                DieFace.Heart,
                DieFace.Heart
            });

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.True(defender.IsAlive);
        Assert.Equal(2, defender.Health);
        Assert.Equal(TokyoSlot.City, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Null(result.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.DoesNotContain(result.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.TargetPlayerId == defender.PlayerId);
        Assert.DoesNotContain(result.NewEvents, e => e is PlayerHealedEvent healed &&
                                                    healed.PlayerId == defender.PlayerId &&
                                                    healed.Reason == "Keep card: Jets.");
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_ClearTokyoAndAwardEater_WhenBurrowingKillsNewOccupantAfterJetsLeaveHealing()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        var eaterOwner = gameState.GetPlayerById(2);
        PutInCity(gameState, defender);
        attacker.TakeDamage(9);
        defender.TakeDamage(3);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Burrowing, "Burrowing", 5));
        eaterOwner.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));
        var engine = CreateEngine(
            diceFaces: new[]
            {
                DieFace.Attack,
                DieFace.Attack,
                DieFace.Three,
                DieFace.One,
                DieFace.Two,
                DieFace.Energy
            },
            camouflageFaces: Array.Empty<DieFace>());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(attacker.IsAlive);
        Assert.Equal(1, attacker.Health);
        Assert.Equal(5, defender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.False(attacker.IsAlive);
        Assert.True(defender.IsAlive);
        Assert.Equal(7, defender.Health);
        Assert.Equal(3, eaterOwner.VictoryPoints);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Null(gameState.Tokyo.CityOccupantId);
        Assert.Contains(leaveResult.NewEvents, e => e is PlayerHealedEvent healed &&
                                                   healed.PlayerId == defender.PlayerId &&
                                                   healed.Amount == 2 &&
                                                   healed.Reason == "Keep card: Jets.");
        Assert.Contains(leaveResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                   damage.SourcePlayerId == defender.PlayerId &&
                                                   damage.TargetPlayerId == attacker.PlayerId &&
                                                   damage.Amount == 1 &&
                                                   damage.DamageKind == DamageKind.CardEffect);
        Assert.Contains(leaveResult.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                                   eliminated.EliminatedPlayerId == attacker.PlayerId &&
                                                   eliminated.EliminatedByPlayerId == defender.PlayerId &&
                                                   eliminated.Reason == "Keep card: Burrowing.");
        Assert.Contains(leaveResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                   gained.PlayerId == eaterOwner.PlayerId &&
                                                   gained.Amount == 3 &&
                                                   gained.Reason == "Keep card: Eater of the Dead.");
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
        var tokyoDecisionService = new TokyoDecisionService(
            damageApplier: damageApplier,
            keepCardRulesService: keepCardRulesService);

        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(diceFaces)),
            finalizeDiceService: finalizeDiceService,
            tokyoDecisionService: tokyoDecisionService,
            keepCardRulesService: keepCardRulesService);
    }

    private static void PutInCity(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);
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
