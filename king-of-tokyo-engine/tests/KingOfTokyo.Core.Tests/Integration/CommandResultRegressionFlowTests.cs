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

public sealed class CommandResultRegressionFlowTests
{
    [Fact]
    public void Execute_Should_ReturnUsefulCommandResultsAcrossSuccessPendingDecisionAndFailurePaths()
    {
        var gameState = CreateGameState(3);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var tokyoDefender = gameState.GetCurrentPlayer();
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);
        gameState.AdvanceToNextAlivePlayer();
        var attacker = gameState.GetCurrentPlayer();
        attacker.GainEnergy(3);

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.Same(gameState, initializeResult.GameState);
        Assert.Empty(initializeResult.NewEvents);
        Assert.Null(initializeResult.PendingDecision);
        Assert.Null(initializeResult.Error);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));

        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.Same(gameState, beginTurnResult.GameState);
        var startedEvent = Assert.Single(beginTurnResult.NewEvents.OfType<TurnStartedEvent>());
        Assert.Equal(attacker.PlayerId, startedEvent.PlayerId);
        Assert.Null(beginTurnResult.PendingDecision);

        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        Assert.True(rollResult.Success, rollResult.Error);
        Assert.Same(gameState, rollResult.GameState);
        Assert.Single(rollResult.NewEvents.OfType<DiceRolledEvent>());
        Assert.NotNull(rollResult.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, rollResult.PendingDecision!.DecisionType);
        Assert.Same(gameState.PendingDecision, rollResult.PendingDecision);

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Same(gameState, finalizeResult.GameState);
        Assert.Contains(finalizeResult.NewEvents, e => e is DiceFinalizedEvent finalized &&
                                                    finalized.PlayerId == attacker.PlayerId);
        Assert.Contains(finalizeResult.NewEvents, e => e is DamageDealtEvent damage &&
                                                    damage.SourcePlayerId == attacker.PlayerId &&
                                                    damage.TargetPlayerId == tokyoDefender.PlayerId &&
                                                    damage.Amount == 2);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, finalizeResult.PendingDecision!.DecisionType);
        Assert.Equal(tokyoDefender.PlayerId, finalizeResult.PendingDecision.PlayerId);
        Assert.Same(gameState.PendingDecision, finalizeResult.PendingDecision);

        var invalidBuyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, attacker.PlayerId));

        Assert.False(invalidBuyResult.Success);
        Assert.Same(gameState, invalidBuyResult.GameState);
        Assert.Empty(invalidBuyResult.NewEvents);
        Assert.Null(invalidBuyResult.PendingDecision);
        Assert.Equal("Cannot buy cards while another decision is pending.", invalidBuyResult.Error);
        Assert.Equal(4, gameState.Version);
        Assert.Same(finalizeResult.PendingDecision, gameState.PendingDecision);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, tokyoDefender.PlayerId));

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.Same(gameState, leaveResult.GameState);
        Assert.Null(leaveResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoLeftEvent left &&
                                                 left.PlayerId == tokyoDefender.PlayerId &&
                                                 left.Slot == TokyoSlot.City);
        Assert.Contains(leaveResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                 entered.PlayerId == attacker.PlayerId &&
                                                 entered.Slot == TokyoSlot.City);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, attacker.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Same(gameState, buyResult.GameState);
        Assert.Null(buyResult.PendingDecision);
        Assert.Contains(buyResult.NewEvents, e => e is CardBoughtEvent bought &&
                                               bought.PlayerId == attacker.PlayerId &&
                                               bought.CardId == KnownCardIds.CornerStore);
        Assert.Contains(buyResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == attacker.PlayerId &&
                                               gained.Amount == 1 &&
                                               gained.Reason == "Bought card: Corner Store.");
        Assert.Equal(6, gameState.Version);
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
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(faces)),
            marketSetupService: new MarketSetupService(new[]
            {
                CreateDiscardCard(KnownCardIds.CornerStore, "Corner Store", 3, new CardPurchaseEffect { GainVictoryPoints = 1 }),
                CreateDiscardCard(KnownCardIds.CommuterTrain, "Commuter Train", 4, new CardPurchaseEffect { GainVictoryPoints = 2 }),
                CreateDiscardCard(KnownCardIds.ApartmentBuilding, "Apartment Building", 5, new CardPurchaseEffect { GainVictoryPoints = 3 }),
                CreateDiscardCard(KnownCardIds.Skyscraper, "Skyscraper", 6, new CardPurchaseEffect { GainVictoryPoints = 4 })
            }));
    }

    private static MarketCardState CreateDiscardCard(string cardId, string name, int cost, CardPurchaseEffect purchaseEffect)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test discard card.",
            cost,
            MarketCardType.Discard,
            purchaseEffect);
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
