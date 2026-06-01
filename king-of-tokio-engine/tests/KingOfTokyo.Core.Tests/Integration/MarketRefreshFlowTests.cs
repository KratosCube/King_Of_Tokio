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

public sealed class MarketRefreshFlowTests
{
    [Fact]
    public void RefreshMarket_Should_SpendTwoEnergy_AndReplaceVisibleCards()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        var beforeIds = gameState.Market.FaceUpCards.Select(card => card!.CardId).ToArray();

        var result = engine.Execute(gameState, new RefreshMarketCommand(0));

        var afterIds = gameState.Market.FaceUpCards.Select(card => card!.CardId).ToArray();

        Assert.True(result.Success);
        Assert.Equal(4, currentPlayer.Energy);
        Assert.Equal(3, gameState.Market.DiscardPileCount);
        Assert.DoesNotContain(beforeIds[0], afterIds);
        Assert.DoesNotContain(beforeIds[1], afterIds);
        Assert.DoesNotContain(beforeIds[2], afterIds);
        Assert.Contains(result.NewEvents, e => e is MarketRefreshedEvent refreshed &&
                                               refreshed.PlayerId == currentPlayer.PlayerId &&
                                               refreshed.CostSpent == 2);
    }

    [Fact]
    public void RefreshMarket_Should_UseMonsterBatteriesStoredEnergy_WhenPlayerEnergyIsInsufficient()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Heart, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        var batteries = CreateMonsterBatteries(storedEnergy: 3);
        currentPlayer.AddKeepCard(batteries);
        var beforeIds = gameState.Market.FaceUpCards.Select(card => card!.CardId).ToArray();

        var result = engine.Execute(gameState, new RefreshMarketCommand(currentPlayer.PlayerId));

        var afterIds = gameState.Market.FaceUpCards.Select(card => card!.CardId).ToArray();

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, currentPlayer.Energy);
        Assert.Equal(2, batteries.StoredEnergy);
        Assert.Contains(batteries, currentPlayer.KeepCards);
        Assert.DoesNotContain(beforeIds[0], afterIds);
        Assert.DoesNotContain(beforeIds[1], afterIds);
        Assert.DoesNotContain(beforeIds[2], afterIds);
        Assert.Contains(result.NewEvents, e => e is MarketRefreshedEvent refreshed &&
                                               refreshed.PlayerId == currentPlayer.PlayerId &&
                                               refreshed.CostSpent == 2);
    }

    [Fact]
    public void RefreshMarket_Should_DiscardMonsterBatteries_WhenRefreshSpendsLastStoredEnergy()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.AddKeepCard(CreateMonsterBatteries(storedEnergy: 2));

        var result = engine.Execute(gameState, new RefreshMarketCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, currentPlayer.Energy);
        Assert.DoesNotContain(currentPlayer.KeepCards, card => card.CardId == KnownCardIds.MonsterBatteries);
        Assert.Contains(gameState.Market.DiscardPile, card =>
            card.CardId == KnownCardIds.MonsterBatteries &&
            card.StoredEnergy == 0);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                               discarded.PlayerId == currentPlayer.PlayerId &&
                                               discarded.CardId == KnownCardIds.MonsterBatteries);
        Assert.Contains(result.NewEvents, e => e is MarketRefreshedEvent);
    }

    [Fact]
    public void RefreshMarket_Should_Fail_WhenPlayerHasNotEnoughEnergy()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Heart, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var result = engine.Execute(gameState, new RefreshMarketCommand(0));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void PurchasePhase_Should_Allow_MultipleActions_InSameTurn()
    {
        var gameState = CreateGameState(4);
        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(1, 0));
        var refreshResult = engine.Execute(gameState, new RefreshMarketCommand(0));

        Assert.True(buyResult.Success);
        Assert.True(refreshResult.Success);
        Assert.Equal(1, currentPlayer.Energy);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
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

    private static MarketCardState CreateMonsterBatteries(int storedEnergy)
    {
        return new MarketCardState(
            KnownCardIds.MonsterBatteries,
            "Monster Batteries",
            "Starts with 6 stored energy. At the end of each turn, lose 2 stored energy. Discard when empty.",
            5,
            MarketCardType.Keep,
            storedEnergy: storedEnergy);
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