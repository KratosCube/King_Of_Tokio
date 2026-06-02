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

public sealed class PsychicProbeCardEffectsFlowTests
{
    [Fact]
    public void ActivatePsychicProbe_Should_RerollCurrentPlayersDie_DuringAnotherPlayersTurn()
    {
        var gameState = CreateGameState(3);
        var activePlayer = gameState.GetCurrentPlayer();
        var psychicPlayer = gameState.GetPlayerById(1);
        psychicPlayer.AddKeepCard(CreatePsychicProbe());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(activePlayer.PlayerId));

        var result = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicPlayer.PlayerId, targetDieIndex: 0));

        Assert.True(result.Success, result.Error);
        Assert.Equal(DieFace.Heart, gameState.CurrentTurn!.DicePool.Dice[0].CurrentFace);
        Assert.Contains(psychicPlayer.KeepCards, card => card.CardId == KnownCardIds.PsychicProbe);
        Assert.DoesNotContain(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.PsychicProbe);
        Assert.Contains(result.NewEvents, e => e is DiceRolledEvent rolled &&
                                               rolled.PlayerId == activePlayer.PlayerId &&
                                               rolled.Faces[0] == DieFace.Heart);
        Assert.NotNull(result.PendingDecision);
    }

    [Fact]
    public void ActivatePsychicProbe_Should_Fail_WhenUsedTwiceDuringSameOtherPlayersTurn()
    {
        var gameState = CreateGameState(3);
        var activePlayer = gameState.GetCurrentPlayer();
        var psychicPlayer = gameState.GetPlayerById(1);
        psychicPlayer.AddKeepCard(CreatePsychicProbe());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Heart,
            DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(activePlayer.PlayerId));

        var firstResult = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicPlayer.PlayerId, targetDieIndex: 0));
        var secondResult = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicPlayer.PlayerId, targetDieIndex: 1));

        Assert.True(firstResult.Success, firstResult.Error);
        Assert.False(secondResult.Success);
        Assert.Contains("once", secondResult.Error);
    }

    [Fact]
    public void ActivatePsychicProbe_Should_ResetUsage_WhenNextTurnBegins()
    {
        var gameState = CreateGameState(3);
        var activePlayer = gameState.GetCurrentPlayer();
        var psychicPlayer = gameState.GetPlayerById(1);
        psychicPlayer.AddKeepCard(CreatePsychicProbe());
        activePlayer.AddKeepCard(CreatePsychicProbe());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Heart,
            DieFace.Two, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Attack);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(activePlayer.PlayerId));
        var firstUseResult = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicPlayer.PlayerId, targetDieIndex: 0));
        engine.Execute(gameState, new FinalizeDiceCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new EndTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new AdvanceToNextPlayerCommand());
        engine.Execute(gameState, new BeginTurnCommand(psychicPlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(psychicPlayer.PlayerId));

        var secondUseResult = engine.Execute(gameState, new ActivatePsychicProbeCommand(activePlayer.PlayerId, targetDieIndex: 0));

        Assert.True(firstUseResult.Success, firstUseResult.Error);
        Assert.True(secondUseResult.Success, secondUseResult.Error);
        Assert.Equal(DieFace.Attack, gameState.CurrentTurn!.DicePool.Dice[0].CurrentFace);
    }

    [Fact]
    public void ActivatePsychicProbe_Should_DiscardCard_WhenRerolledFaceIsEnergy()
    {
        var gameState = CreateGameState(3);
        var activePlayer = gameState.GetCurrentPlayer();
        var psychicPlayer = gameState.GetPlayerById(1);
        psychicPlayer.AddKeepCard(CreatePsychicProbe());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(activePlayer.PlayerId));

        var result = engine.Execute(gameState, new ActivatePsychicProbeCommand(psychicPlayer.PlayerId, targetDieIndex: 0));

        Assert.True(result.Success, result.Error);
        Assert.Equal(DieFace.Energy, gameState.CurrentTurn!.DicePool.Dice[0].CurrentFace);
        Assert.DoesNotContain(psychicPlayer.KeepCards, card => card.CardId == KnownCardIds.PsychicProbe);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.PsychicProbe);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                               discarded.PlayerId == psychicPlayer.PlayerId &&
                                               discarded.CardId == KnownCardIds.PsychicProbe);
    }

    [Fact]
    public void ActivatePsychicProbe_Should_Fail_DuringOwnTurn()
    {
        var gameState = CreateGameState(3);
        var activePlayer = gameState.GetCurrentPlayer();
        activePlayer.AddKeepCard(CreatePsychicProbe());
        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Attack, DieFace.Attack,
            DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(activePlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(activePlayer.PlayerId));

        var result = engine.Execute(gameState, new ActivatePsychicProbeCommand(activePlayer.PlayerId, targetDieIndex: 0));

        Assert.False(result.Success);
        Assert.Contains("another player's turn", result.Error);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreatePsychicProbe()
    {
        return new MarketCardState(
            KnownCardIds.PsychicProbe,
            "Psychic Probe",
            "Once during each other player's turn, reroll one of that player's dice. If the rerolled die is energy, discard this card.",
            3,
            MarketCardType.Keep);
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
