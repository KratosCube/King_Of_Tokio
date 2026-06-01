using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
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

public sealed class FrenzyCardEffectsFlowTests
{
    [Fact]
    public void BuyFaceUpCard_Should_ScheduleImmediateExtraTurn_WhenBuyingFrenzy()
    {
        var frenzy = new MarketCardState(
            KnownCardIds.Frenzy,
            "Frenzy",
            "After buying this card, immediately take one extra turn.",
            7,
            MarketCardType.Discard);
        var gameState = CreateGameState(3);
        var marketSetupService = new MarketSetupService(new[]
        {
            frenzy,
            new MarketCardState(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { Heal = 2 }),
            new MarketCardState(KnownCardIds.ApartmentBuilding, "Apartment Building", "+3 victory points.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 3 })
        });
        var engine = new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(
                DieFace.Energy, DieFace.Energy, DieFace.Energy,
                DieFace.Energy, DieFace.Energy, DieFace.Energy)),
            marketSetupService: marketSetupService);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(0));
        engine.Execute(gameState, new RollDiceCommand(0));
        engine.Execute(gameState, new FinalizeDiceCommand(0));

        var currentPlayer = gameState.GetCurrentPlayer();
        currentPlayer.GainEnergy(1);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, currentPlayer.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(0, currentPlayer.Energy);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.Frenzy);
        Assert.Contains(buyResult.NewEvents, e => e is CardBoughtEvent bought &&
                                                  bought.PlayerId == currentPlayer.PlayerId &&
                                                  bought.CardId == KnownCardIds.Frenzy &&
                                                  bought.CostSpent == 7);

        var scheduledTurn = Assert.Single(gameState.ScheduledTurns);
        Assert.Equal(currentPlayer.PlayerId, scheduledTurn.PlayerId);
        Assert.Equal(0, scheduledTurn.DiceCountModifier);

        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));
        Assert.True(endTurnResult.Success, endTurnResult.Error);

        var advanceResult = engine.Execute(gameState, new AdvanceToNextPlayerCommand());
        Assert.True(advanceResult.Success, advanceResult.Error);
        Assert.Equal(currentPlayer.PlayerId, gameState.GetCurrentPlayer().PlayerId);

        var beginExtraTurnResult = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));

        Assert.True(beginExtraTurnResult.Success, beginExtraTurnResult.Error);
        Assert.True(gameState.CurrentTurn!.IsExtraTurn);
        Assert.Equal(0, gameState.CurrentTurn.DiceCountModifier);
        Assert.Equal(6, gameState.CurrentTurn.DiceCount);
        Assert.Empty(gameState.ScheduledTurns);
        Assert.Equal(0, gameState.NextTurnDiceCountModifier);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(params DieFace[] faces)
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
