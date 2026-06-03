using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicMetamorphCleanupFlowTests
{
    [Fact]
    public void ActivateMetamorph_Should_ClearMimicTarget_WhenCopiedCardIsDiscarded()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var mimicOwner = gameState.GetPlayerById(1);
        var discardedCard = CreateKeepCard(KnownCardIds.ExtraHead, "Extra Head", 7);
        var mimic = CreateMimicCopying(player.PlayerId, KnownCardIds.ExtraHead, "Extra Head");

        player.AddKeepCard(CreateKeepCard(KnownCardIds.Metamorph, "Metamorph", 3));
        player.AddKeepCard(discardedCard);
        mimicOwner.AddKeepCard(mimic);

        var engine = new GameEngine();
        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        gameState.CurrentTurn!.MarkDiceResolved();
        gameState.CurrentTurn.SetPhase(TurnPhase.Purchase);

        var result = engine.Execute(gameState, new ActivateMetamorphCommand(discardedCard.CardId, player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Null(mimic.MimicTarget);
        Assert.False(player.HasKeepCard(KnownCardIds.ExtraHead));
        Assert.True(player.HasKeepCard(KnownCardIds.Metamorph));
        Assert.Equal(7, player.Energy);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.ExtraHead, gameState.Market.DiscardPile[0].CardId);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                             discarded.PlayerId == player.PlayerId &&
                                             discarded.CardId == KnownCardIds.ExtraHead);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
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
}
