using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicMadeInALabFlowTests
{
    [Fact]
    public void CanUseMadeInALab_Should_ReturnTrue_WhenMimicCopiesMadeInALab()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.MadeInALab, "Made in a Lab"));
        var service = new KeepCardRulesService();

        var canUseMadeInALab = service.CanUseMadeInALab(player);

        Assert.True(canUseMadeInALab);
    }

    [Fact]
    public void PeekTopDeckCard_Should_Work_WhenPlayerMimicsMadeInALab()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.MadeInALab, "Made in a Lab"));

        var engine = CreateEngineWithDeck(CreateDeckWithHealOnTop());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new PeekTopDeckCardCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.PeekTopDeckCardPurchase, result.PendingDecision!.DecisionType);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.MadeInALab));

        var payload = Assert.IsType<PeekTopDeckCardDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(KnownCardIds.Heal, payload.CardId);
        Assert.Equal(3, payload.EffectiveCost);
    }

    [Fact]
    public void BuyPeekedTopDeckCard_Should_Work_WhenPlayerMimicsMadeInALab()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.MadeInALab, "Made in a Lab"));
        player.GainEnergy(3);
        player.TakeDamage(3);

        var engine = CreateEngineWithDeck(CreateDeckWithHealOnTop());

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));
        engine.Execute(gameState, new PeekTopDeckCardCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyPeekedTopDeckCardCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, player.Energy);
        Assert.Equal(9, player.Health);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.MadeInALab));
        Assert.Null(gameState.PendingDecision);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.Heal, gameState.Market.DiscardPile[0].CardId);
        Assert.Equal(0, gameState.Market.DrawPileCount);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static IReadOnlyList<MarketCardState> CreateDeckWithHealOnTop()
    {
        return new[]
        {
            new MarketCardState("faceup-001", "Faceup 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-002", "Faceup 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-003", "Faceup 3", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard,
                new CardPurchaseEffect { Heal = 2 })
        };
    }

    private static GameEngine CreateEngineWithDeck(IReadOnlyList<MarketCardState> deck)
    {
        return new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));
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
