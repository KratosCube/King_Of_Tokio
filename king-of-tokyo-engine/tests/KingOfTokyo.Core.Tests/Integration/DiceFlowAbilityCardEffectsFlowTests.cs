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

public sealed class DiceFlowAbilityCardEffectsFlowTests
{
    [Fact]
    public void BeginTurn_Should_UseSevenDice_WhenPlayerHasExtraHead()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.ExtraHead,
            "Extra Head",
            "You have 1 extra die.",
            7,
            MarketCardType.Keep));

        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        var result = engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.NotNull(gameState.CurrentTurn);
        Assert.Equal(7, gameState.CurrentTurn!.DicePool.Dice.Count);
    }

    [Fact]
    public void ActivateTelepath_Should_SpendOneEnergy_AndIncreaseMaxRolls()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Telepath,
            "Telepath",
            "Spend 1 energy to gain 1 extra reroll.",
            4,
            MarketCardType.Keep));
        player.GainEnergy(1);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateTelepathCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(0, player.Energy);
        Assert.Equal(4, gameState.CurrentTurn!.MaxRolls);
        Assert.NotNull(gameState.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, gameState.PendingDecision!.DecisionType);
    }

    [Fact]
    public void ActivateStretchy_Should_SpendTwoEnergy_AndChangeSelectedDieFace()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Stretchy,
            "Stretchy",
            "Spend 2 energy to change one of your dice to any result.",
            3,
            MarketCardType.Keep));
        player.GainEnergy(2);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.One,
            DieFace.Two, DieFace.Heart, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateStretchyCommand(2, DieFace.Attack, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(0, player.Energy);
        Assert.Equal(DieFace.Attack, gameState.CurrentTurn!.DicePool.Dice[2].CurrentFace);
    }

    [Fact]
    public void ActivateHerdCuller_Should_ChangeSelectedDieToOne_OncePerTurn()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.HerdCuller,
            "Herd Culler",
            "Once per turn, you may change one die to 1.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Attack,
            DieFace.Two, DieFace.Heart, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateHerdCullerCommand(2, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(DieFace.One, gameState.CurrentTurn!.DicePool.Dice[2].CurrentFace);
        Assert.True(gameState.CurrentTurn.Flags.HerdCullerUsed);

        result = engine.Execute(gameState, new ActivateHerdCullerCommand(3, player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal(DieFace.Two, gameState.CurrentTurn.DicePool.Dice[3].CurrentFace);
    }

    [Fact]
    public void ActivateMetamorph_Should_DiscardOwnedKeepCard_AndGainEnergyEqualToCost()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Metamorph,
            "Metamorph",
            "At the end of your turn, you may discard one of your keep cards to gain energy equal to its cost.",
            3,
            MarketCardType.Keep));
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.ExtraHead,
            "Extra Head",
            "You have 1 extra die.",
            7,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Heart, DieFace.Energy,
            DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateMetamorphCommand(KnownCardIds.ExtraHead, player.PlayerId));

        Assert.True(result.Success);
        Assert.False(player.HasKeepCard(KnownCardIds.ExtraHead));
        Assert.True(player.HasKeepCard(KnownCardIds.Metamorph));
        Assert.Equal(8, player.Energy);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.ExtraHead, gameState.Market.DiscardPile[0].CardId);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                               discarded.PlayerId == player.PlayerId &&
                                               discarded.CardId == KnownCardIds.ExtraHead);
    }

    [Fact]
    public void ActivateMetamorph_Should_ApplyEvenBiggerLossEffect_WhenDiscardingEvenBigger()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Metamorph,
            "Metamorph",
            "At the end of your turn, you may discard one of your keep cards to gain energy equal to its cost.",
            3,
            MarketCardType.Keep));
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.EvenBigger,
            "Even Bigger",
            "Your maximum health is increased by 2.",
            8,
            MarketCardType.Keep));
        player.IncreaseMaxHealth(2);
        player.Heal(2);

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateMetamorphCommand(KnownCardIds.EvenBigger, player.PlayerId));

        Assert.True(result.Success);
        Assert.False(player.HasKeepCard(KnownCardIds.EvenBigger));
        Assert.Equal(10, player.MaxHealth);
        Assert.Equal(10, player.Health);
        Assert.Equal(9, player.Energy);
    }

    [Fact]
    public void PeekTopDeckCard_Should_CreatePendingDecision_WhenPlayerHasMadeInALab()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.MadeInALab,
            "Made in a Lab",
            "During your purchase phase, you may look at the top card of the deck and buy it.",
            2,
            MarketCardType.Keep));

        var deck = new[]
        {
            new MarketCardState("faceup-001", "Faceup 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-002", "Faceup 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-003", "Faceup 3", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard,
                new CardPurchaseEffect { Heal = 2 })
        };

        var engine = new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new PeekTopDeckCardCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.NotNull(result.PendingDecision);
        Assert.Equal(DecisionType.PeekTopDeckCardPurchase, result.PendingDecision!.DecisionType);

        var payload = Assert.IsType<PeekTopDeckCardDecisionData>(result.PendingDecision.Payload);
        Assert.Equal(KnownCardIds.Heal, payload.CardId);
        Assert.Equal(3, payload.EffectiveCost);
    }

    [Fact]
    public void BuyPeekedTopDeckCard_Should_BuyTopDeckCard_WhenPlayerHasMadeInALab()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.MadeInALab,
            "Made in a Lab",
            "During your purchase phase, you may look at the top card of the deck and buy it.",
            2,
            MarketCardType.Keep));

        player.GainEnergy(3);
        player.TakeDamage(3);

        var deck = new[]
        {
            new MarketCardState("faceup-001", "Faceup 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-002", "Faceup 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-003", "Faceup 3", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard,
                new CardPurchaseEffect { Heal = 2 })
        };

        var engine = new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));
        engine.Execute(gameState, new PeekTopDeckCardCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyPeekedTopDeckCardCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(0, player.Energy);
        Assert.Equal(9, player.Health);
        Assert.Null(gameState.PendingDecision);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.Heal, gameState.Market.DiscardPile[0].CardId);
        Assert.Equal(0, gameState.Market.DrawPileCount);
    }

    [Fact]
    public void DeclinePeekedTopDeckCard_Should_ClearPendingDecision_AndKeepTopCard()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.MadeInALab,
            "Made in a Lab",
            "During your purchase phase, you may look at the top card of the deck and buy it.",
            2,
            MarketCardType.Keep));

        var deck = new[]
        {
            new MarketCardState("faceup-001", "Faceup 1", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-002", "Faceup 2", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState("faceup-003", "Faceup 3", "No effect.", 1, MarketCardType.Keep),
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard,
                new CardPurchaseEffect { Heal = 2 })
        };

        var engine = new GameEngine(
            marketSetupService: new MarketSetupService(deck),
            diceRollService: new DiceRollService(new SequenceRandomSource(new[]
            {
                DieFace.One, DieFace.Two, DieFace.Three,
                DieFace.One, DieFace.Two, DieFace.Three
            })));

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));
        engine.Execute(gameState, new PeekTopDeckCardCommand(player.PlayerId));

        var result = engine.Execute(gameState, new DeclinePeekedTopDeckCardCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(1, gameState.Market.DrawPileCount);
        Assert.Equal(KnownCardIds.Heal, gameState.Market.PeekTopDrawCard().CardId);
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