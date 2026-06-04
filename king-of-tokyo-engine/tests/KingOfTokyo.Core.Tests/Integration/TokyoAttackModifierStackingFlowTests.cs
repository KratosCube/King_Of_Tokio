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

public sealed class TokyoAttackModifierStackingFlowTests
{
    [Fact]
    public void FinalizeDice_Should_StackAcidAttackSpikedTailAndBurrowing_WhenOutsidePlayerAttacksTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail", 5));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.Burrowing, "Burrowing", 5));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, defender.Health);
        Assert.NotNull(result.PendingDecision);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == defender.PlayerId &&
                                             damage.Amount == 4 &&
                                             damage.DamageKind == DamageKind.Attack);
    }

    [Fact]
    public void FinalizeDice_Should_StackAcidAttackSpikedTailAndUrbavore_WhenTokyoPlayerAttacksOutsideMonsters()
    {
        var gameState = CreateGameState(4);
        var attacker = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        var targetC = gameState.GetPlayerById(3);
        attacker.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(attacker.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail", 5));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.Urbavore, "Urbavore", 4));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(6, targetA.Health);
        Assert.Equal(6, targetB.Health);
        Assert.Equal(6, targetC.Health);
        Assert.Null(result.PendingDecision);
        Assert.Equal(3, result.NewEvents.OfType<DamageDealtEvent>().Count(damage =>
            damage.SourcePlayerId == attacker.PlayerId &&
            damage.Amount == 4 &&
            damage.DamageKind == DamageKind.Attack));
    }

    [Fact]
    public void FinalizeDice_Should_NotApplyBurrowingBonus_WhenTokyoPlayerAttacksOutsideMonsters()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var targetA = gameState.GetPlayerById(1);
        var targetB = gameState.GetPlayerById(2);
        attacker.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(attacker.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.Burrowing, "Burrowing", 5));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

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
