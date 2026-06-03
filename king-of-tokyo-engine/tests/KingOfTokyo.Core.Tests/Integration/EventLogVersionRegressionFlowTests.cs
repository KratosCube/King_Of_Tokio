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

public sealed class EventLogVersionRegressionFlowTests
{
    [Fact]
    public void SuccessfulCommands_Should_IncrementVersionAndAppendEventsAcrossCombatAndPurchaseFlow()
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

        AssertSuccessfulCommandState(gameState, initializeResult, expectedVersion: 1);
        Assert.Empty(gameState.EventLog);

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        AssertSuccessfulCommandState(gameState, beginTurnResult, expectedVersion: 2);
        Assert.Contains(gameState.EventLog, e => e is TurnStartedEvent started &&
                                              started.PlayerId == attacker.PlayerId);

        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        AssertSuccessfulCommandState(gameState, rollResult, expectedVersion: 3);
        Assert.Contains(gameState.EventLog, e => e is DiceRolledEvent rolled &&
                                              rolled.PlayerId == attacker.PlayerId);

        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));
        AssertSuccessfulCommandState(gameState, finalizeResult, expectedVersion: 4);
        Assert.NotNull(finalizeResult.PendingDecision);
        Assert.Contains(gameState.EventLog, e => e is DiceFinalizedEvent finalized &&
                                              finalized.PlayerId == attacker.PlayerId);
        Assert.Contains(gameState.EventLog, e => e is DamageDealtEvent damage &&
                                              damage.SourcePlayerId == attacker.PlayerId &&
                                              damage.TargetPlayerId == tokyoDefender.PlayerId &&
                                              damage.Amount == 2);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, tokyoDefender.PlayerId));
        AssertSuccessfulCommandState(gameState, leaveResult, expectedVersion: 5);
        Assert.Null(gameState.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Contains(gameState.EventLog, e => e is TokyoLeftEvent left &&
                                              left.PlayerId == tokyoDefender.PlayerId &&
                                              left.Slot == TokyoSlot.City);
        Assert.Contains(gameState.EventLog, e => e is TokyoEnteredEvent entered &&
                                              entered.PlayerId == attacker.PlayerId &&
                                              entered.Slot == TokyoSlot.City);

        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, attacker.PlayerId));
        AssertSuccessfulCommandState(gameState, buyResult, expectedVersion: 6);
        Assert.Equal(1, attacker.Energy);
        Assert.Equal(2, attacker.VictoryPoints);
        Assert.Contains(gameState.EventLog, e => e is CardBoughtEvent bought &&
                                              bought.PlayerId == attacker.PlayerId &&
                                              bought.CardId == KnownCardIds.CornerStore &&
                                              bought.Cost == 3);
        Assert.Contains(gameState.EventLog, e => e is VictoryPointsGainedEvent gained &&
                                              gained.PlayerId == attacker.PlayerId &&
                                              gained.Amount == 1 &&
                                              gained.Reason == "Bought card: Corner Store.");
        Assert.True(gameState.EventLog.Count >= 8);
    }

    private static void AssertSuccessfulCommandState(GameState gameState, CommandResult result, long expectedVersion)
    {
        Assert.True(result.Success, result.Error);
        Assert.Equal(expectedVersion, gameState.Version);
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
