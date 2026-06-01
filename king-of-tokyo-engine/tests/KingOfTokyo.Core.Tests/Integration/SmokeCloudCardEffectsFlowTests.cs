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

public sealed class SmokeCloudCardEffectsFlowTests
{
    [Fact]
    public void ActivateSmokeCloud_Should_SpendOneChargeAndAddOneExtraReroll()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var smokeCloud = CreateSmokeCloud(counters: 3);
        player.AddKeepCard(smokeCloud);
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        var maxRollsBeforeActivation = gameState.CurrentTurn!.MaxRolls;

        var result = engine.Execute(gameState, new ActivateSmokeCloudCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, smokeCloud.Counters);
        Assert.Equal(maxRollsBeforeActivation + 1, gameState.CurrentTurn.MaxRolls);
        Assert.Contains(smokeCloud, player.KeepCards);
        Assert.DoesNotContain(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.SmokeCloud);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, result.PendingDecision!.DecisionType);

        var payload = Assert.IsType<RerollDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(maxRollsBeforeActivation + 1, payload.MaxRolls);
    }

    [Fact]
    public void ActivateSmokeCloud_Should_DiscardCard_WhenLastChargeIsSpent()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateSmokeCloud(counters: 1));
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateSmokeCloudCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain(player.KeepCards, card => card.CardId == KnownCardIds.SmokeCloud);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.SmokeCloud && card.Counters == 0);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                               discarded.PlayerId == player.PlayerId &&
                                               discarded.CardId == KnownCardIds.SmokeCloud);
    }

    [Fact]
    public void ActivateSmokeCloud_Should_Fail_WhenCardHasNoCharges()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateSmokeCloud(counters: 0));
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Energy, DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateSmokeCloudCommand(player.PlayerId));

        Assert.False(result.Success);
        Assert.Contains(player.KeepCards, card => card.CardId == KnownCardIds.SmokeCloud && card.Counters == 0);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateSmokeCloud(int counters)
    {
        return new MarketCardState(
            KnownCardIds.SmokeCloud,
            "Smoke Cloud",
            "Starts with 3 charges. Spend 1 charge to gain 1 extra reroll. Discard after all charges are used.",
            4,
            MarketCardType.Keep,
            counters: counters);
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
