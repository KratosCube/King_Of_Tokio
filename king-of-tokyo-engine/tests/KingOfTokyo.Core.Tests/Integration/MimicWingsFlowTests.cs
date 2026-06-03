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
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicWingsFlowTests
{
    [Fact]
    public void CanUseWings_Should_ReturnTrue_WhenMimicCopiesWingsAndPlayerHasEnergy()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Wings, "Wings"));
        player.GainEnergy(KeepCardRulesService.WingsCost);
        var service = new KeepCardRulesService();

        var canUseWings = service.CanUseWings(player);

        Assert.True(canUseWings);
    }

    [Fact]
    public void ActivateWings_Should_Work_WhenPlayerMimicsWings()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        defender.GainEnergy(KeepCardRulesService.WingsCost);
        defender.AddKeepCard(CreateMimicCopying(KnownCardIds.Wings, "Wings"));
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
        Assert.Equal(KeepCardRulesService.WingsCost, defender.Energy);
        Assert.NotNull(gameState.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, gameState.PendingDecision!.DecisionType);

        var wingsResult = engine.Execute(gameState, new ActivateWingsCommand(defender.PlayerId));

        Assert.True(wingsResult.Success, wingsResult.Error);
        Assert.Equal(10, defender.Health);
        Assert.Equal(0, defender.Energy);
        Assert.True(defender.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(defender.HasKeepCard(KnownCardIds.Wings));
        Assert.NotNull(wingsResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, wingsResult.PendingDecision!.DecisionType);
        var leavePayload = Assert.IsType<LeaveTokyoDecisionData>(wingsResult.PendingDecision.Payload);
        Assert.Equal(0, leavePayload.DamageTaken);
        Assert.Contains(wingsResult.NewEvents, e => e is DamageCanceledEvent canceled &&
                                                   canceled.PlayerId == defender.PlayerId &&
                                                   canceled.Amount == 2);
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

    private static MarketCardState CreateMimicCopying(string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(1, copiedCardId, copiedCardName));
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
