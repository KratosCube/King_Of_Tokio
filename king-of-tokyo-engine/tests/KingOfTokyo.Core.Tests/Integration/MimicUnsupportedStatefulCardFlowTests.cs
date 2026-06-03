using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicUnsupportedStatefulCardFlowTests
{
    [Fact]
    public void ActivateSmokeCloud_Should_Fail_WhenPlayerOnlyMimicsSmokeCloud()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.SmokeCloud, "Smoke Cloud"));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateSmokeCloudCommand(player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use Smoke Cloud right now.", result.Error);
        Assert.Equal(3, gameState.CurrentTurn!.MaxRolls);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.SmokeCloud));
    }

    [Fact]
    public void ActivatePlotTwist_Should_Fail_WhenPlayerOnlyMimicsPlotTwist()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.PlotTwist, "Plot Twist"));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivatePlotTwistCommand(0, DieFace.Attack, player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use Plot Twist right now.", result.Error);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.PlotTwist));
        Assert.Empty(gameState.Market.DiscardPile);
    }

    [Fact]
    public void ActivateMetamorph_Should_Fail_WhenPlayerOnlyMimicsMetamorph()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Metamorph, "Metamorph"));
        player.AddKeepCard(CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5));
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateMetamorphCommand(KnownCardIds.GiantBrain, player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Player cannot use Metamorph right now.", result.Error);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.True(player.HasKeepCard(KnownCardIds.GiantBrain));
        Assert.False(player.HasKeepCard(KnownCardIds.Metamorph));
        Assert.Empty(gameState.Market.DiscardPile);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] faces)
    {
        return new GameEngine(diceRollService: new DiceRollService(new SequenceRandomSource(faces)));
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
