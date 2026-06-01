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

public sealed class WingsCardEffectsFlowTests
{
    [Fact]
    public void ActivateWings_Should_CancelDamageTakenThisTurn_AndKeepTokyoLeaveDecision()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        defender.GainEnergy(2);
        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.Wings,
            "Wings",
            "Spend 2 energy to cancel damage you took this turn.",
            6,
            MarketCardType.Keep));
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Attack, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, defender.Health);
        Assert.Equal(2, defender.Energy);
        Assert.NotNull(gameState.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, gameState.PendingDecision!.DecisionType);
        var leavePayload = Assert.IsType<LeaveTokyoDecisionData>(gameState.PendingDecision.Payload);
        Assert.Equal(2, leavePayload.DamageTaken);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(defender.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, defender.Health);
        Assert.Equal(0, defender.Energy);
        Assert.NotNull(wingsResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, wingsResult.PendingDecision!.DecisionType);
        leavePayload = Assert.IsType<LeaveTokyoDecisionData>(wingsResult.PendingDecision.Payload);
        Assert.Equal(0, leavePayload.DamageTaken);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == defender.PlayerId &&
                                                   canceled.Amount == 2);
    }

    [Fact]
    public void ActivateWings_Should_Fail_WhenPlayerHasNotTakenDamageThisTurn()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetPlayerById(0);
        player.GainEnergy(2);
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Wings,
            "Wings",
            "Spend 2 energy to cancel damage you took this turn.",
            6,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Energy, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(player.PlayerId));

        Assert.False(wingsResult.Success);
        Assert.Equal(2, player.Energy);
        Assert.Equal(10, player.Health);
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
