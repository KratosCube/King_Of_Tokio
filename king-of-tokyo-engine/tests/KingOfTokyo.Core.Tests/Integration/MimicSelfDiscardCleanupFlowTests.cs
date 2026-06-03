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

public sealed class MimicSelfDiscardCleanupFlowTests
{
    [Fact]
    public void ActivatePlotTwist_Should_ClearMimicTarget_WhenPlotTwistIsDiscarded()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var mimicOwner = gameState.GetPlayerById(1);
        var mimic = CreateMimicCopying(player.PlayerId, KnownCardIds.PlotTwist, "Plot Twist");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.PlotTwist, "Plot Twist", 3));
        mimicOwner.AddKeepCard(mimic);

        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivatePlotTwistCommand(0, DieFace.Attack, player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Null(mimic.MimicTarget);
        Assert.False(player.HasKeepCard(KnownCardIds.PlotTwist));
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.PlotTwist, gameState.Market.DiscardPile[0].CardId);
    }

    [Fact]
    public void ActivateSmokeCloud_Should_ClearMimicTarget_WhenSmokeCloudIsDiscarded()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var mimicOwner = gameState.GetPlayerById(1);
        var mimic = CreateMimicCopying(player.PlayerId, KnownCardIds.SmokeCloud, "Smoke Cloud");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.SmokeCloud, "Smoke Cloud", 4, counters: 1));
        mimicOwner.AddKeepCard(mimic);

        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateSmokeCloudCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Null(mimic.MimicTarget);
        Assert.False(player.HasKeepCard(KnownCardIds.SmokeCloud));
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.SmokeCloud, gameState.Market.DiscardPile[0].CardId);
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

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost, int counters = 0)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep,
            counters: counters);
    }

    private static MarketCardState CreateMimicCopying(int ownerPlayerId, string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(ownerPlayerId, copiedCardId, copiedCardName));
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
