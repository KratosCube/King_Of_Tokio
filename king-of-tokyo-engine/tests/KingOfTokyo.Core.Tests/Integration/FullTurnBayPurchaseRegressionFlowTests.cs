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

public sealed class FullTurnBayPurchaseRegressionFlowTests
{
    [Fact]
    public void TurnFlow_Should_HandleBayLeaveQueueThenPurchaseCard()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        attacker.GainEnergy(3);
        cityOccupant.SetTokyoSlot(TokyoSlot.City);
        bayOccupant.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityOccupant.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayOccupant.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Attack,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(8, cityOccupant.Health);
        Assert.Equal(8, bayOccupant.Health);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, finalizeResult.PendingDecision!.DecisionType);
        var cityDecisionPayload = Assert.IsType<LeaveTokyoDecisionData>(finalizeResult.PendingDecision.Payload);
        Assert.Equal(cityOccupant.PlayerId, cityDecisionPayload.DefenderPlayerId);

        var cityDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, cityOccupant.PlayerId));

        Assert.True(cityDecisionResult.Success, cityDecisionResult.Error);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(TokyoSlot.None, cityOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.NotNull(cityDecisionResult.PendingDecision);
        var bayDecisionPayload = Assert.IsType<LeaveTokyoDecisionData>(cityDecisionResult.PendingDecision!.Payload);
        Assert.Equal(bayOccupant.PlayerId, bayDecisionPayload.DefenderPlayerId);
        Assert.Contains(cityDecisionResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                        entered.PlayerId == attacker.PlayerId &&
                                                        entered.Slot == TokyoSlot.City);

        var bayDecisionResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(false, bayOccupant.PlayerId));

        Assert.True(bayDecisionResult.Success, bayDecisionResult.Error);
        Assert.Null(bayDecisionResult.PendingDecision);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, bayOccupant.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(bayOccupant.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.DoesNotContain(bayDecisionResult.NewEvents, e => e is TokyoEnteredEvent);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, attacker.PlayerId));

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(0, attacker.Energy);
        Assert.Equal(2, attacker.VictoryPoints);
        Assert.Contains(buyResult.NewEvents, e => e is EnergySpentEvent spent &&
                                               spent.PlayerId == attacker.PlayerId &&
                                               spent.Amount == 3);
        Assert.Contains(buyResult.NewEvents, e => e is CardBoughtEvent bought &&
                                               bought.PlayerId == attacker.PlayerId &&
                                               bought.CardId == KnownCardIds.CornerStore &&
                                               bought.Cost == 3);
        Assert.Contains(buyResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == attacker.PlayerId &&
                                               gained.Amount == 1 &&
                                               gained.Reason == "Bought card: Corner Store.");
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
