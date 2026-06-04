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

public sealed class JetsBurrowingTokyoLeaveFlowTests
{
    [Fact]
    public void ChooseLeaveTokyo_Should_HealDefenderWithJets_AndStillApplyBurrowingDamageToNewOccupant()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Burrowing, "Burrowing", 5));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, defender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(10, defender.Health);
        Assert.Equal(9, attacker.Health);
        Assert.Contains(leaveResult.NewEvents, e => e is PlayerHealedEvent healed &&
                                                   healed.PlayerId == defender.PlayerId &&
                                                   healed.Amount == 2 &&
                                                   healed.Reason == "Keep card: Jets.");
        Assert.Contains(leaveResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                   damage.SourcePlayerId == defender.PlayerId &&
                                                   damage.TargetPlayerId == attacker.PlayerId &&
                                                   damage.Amount == 1 &&
                                                   damage.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_NotHealWithJets_WhenDefenderStaysInTokyo()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Jets, "Jets", 5));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, defender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);

        var stayResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, defender.PlayerId));

        Assert.True(stayResult.Success, stayResult.Error);
        Assert.Equal(TokyoSlot.City, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.None, attacker.TokyoSlot);
        Assert.Equal(8, defender.Health);
        Assert.DoesNotContain(stayResult.NewEvents, e => e is PlayerHealedEvent healed &&
                                                        healed.PlayerId == defender.PlayerId &&
                                                        healed.Reason == "Keep card: Jets.");
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_ApplyBurrowingDamageAfterWingsCanceledLeaveDamage()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        PutInCity(gameState, defender);
        defender.GainEnergy(2);
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Wings, "Wings", 6));
        defender.AddKeepCard(CreateKeepCard(KnownCardIds.Burrowing, "Burrowing", 5));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, defender.Health);
        Assert.NotNull(finalizeResult.PendingDecision);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(defender.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, defender.Health);
        Assert.NotNull(wingsResult.PendingDecision);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Equal(TokyoSlot.None, defender.TokyoSlot);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(9, attacker.Health);
        Assert.Contains(leaveResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                   damage.SourcePlayerId == defender.PlayerId &&
                                                   damage.TargetPlayerId == attacker.PlayerId &&
                                                   damage.Amount == 1 &&
                                                   damage.DamageKind == DamageKind.CardEffect);
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
