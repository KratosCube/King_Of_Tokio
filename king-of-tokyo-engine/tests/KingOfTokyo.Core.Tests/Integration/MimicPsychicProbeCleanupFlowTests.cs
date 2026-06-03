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

public sealed class MimicPsychicProbeCleanupFlowTests
{
    [Fact]
    public void ActivatePsychicProbe_Should_ClearMimicTarget_WhenPsychicProbeIsDiscarded()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();
        var psychicProbeOwner = gameState.GetPlayerById(1);
        var mimicOwner = gameState.GetPlayerById(2);
        var psychicProbe = CreateKeepCard(KnownCardIds.PsychicProbe, "Psychic Probe", 3);
        var mimic = CreateMimicCopying(psychicProbeOwner.PlayerId, KnownCardIds.PsychicProbe, "Psychic Probe");
        psychicProbeOwner.AddKeepCard(psychicProbe);
        mimicOwner.AddKeepCard(mimic);

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));

        var result = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicProbeOwner.PlayerId, 0));

        Assert.True(result.Success, result.Error);
        Assert.Null(mimic.MimicTarget);
        Assert.False(psychicProbeOwner.HasKeepCard(KnownCardIds.PsychicProbe));
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.PsychicProbe, gameState.Market.DiscardPile[0].CardId);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                             discarded.PlayerId == psychicProbeOwner.PlayerId &&
                                             discarded.CardId == KnownCardIds.PsychicProbe);
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
