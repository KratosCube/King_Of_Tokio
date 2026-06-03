using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class DtoSnapshotRegressionFlowTests
{
    [Fact]
    public void ToDto_Should_ProjectCombatDecisionAndPurchaseStateAcrossTurnFlow()
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

        var beginTurnResult = engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));
        var decisionDto = gameState.ToDto();

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginTurnResult.Success, beginTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.Equal(gameState.GameId, decisionDto.GameId);
        Assert.Equal(gameState.Version, decisionDto.Version);
        Assert.Equal(GameStatus.Running, decisionDto.Status);
        Assert.Equal(attacker.PlayerId, decisionDto.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, decisionDto.CurrentTurn.Phase);
        Assert.Equal(1, decisionDto.CurrentTurn.RollCountUsed);
        Assert.True(decisionDto.CurrentTurn.DiceResolved);
        Assert.Equal(6, decisionDto.CurrentTurn.Dice.Count);
        Assert.True(decisionDto.CurrentTurn.Flags.AttackedWithDice);
        Assert.True(decisionDto.CurrentTurn.Flags.DealtDamage);
        Assert.False(decisionDto.CurrentTurn.Flags.EnteredTokyo);
        Assert.Equal(tokyoDefender.PlayerId, decisionDto.Tokyo.CityOccupantId);
        Assert.Null(decisionDto.Tokyo.BayOccupantId);
        Assert.False(decisionDto.Tokyo.BayEnabled);
        Assert.NotNull(decisionDto.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, decisionDto.PendingDecision!.DecisionType);
        Assert.Equal(tokyoDefender.PlayerId, decisionDto.PendingDecision.PlayerId);
        var payload = Assert.IsType<LeaveTokyoDecisionData>(decisionDto.PendingDecision.Payload);
        Assert.Equal(attacker.PlayerId, payload.AttackerPlayerId);
        Assert.Equal(tokyoDefender.PlayerId, payload.DefenderPlayerId);
        Assert.Equal(2, payload.DamageTaken);
        Assert.Equal(8, decisionDto.Players.Single(player => player.PlayerId == tokyoDefender.PlayerId).Health);
        Assert.Equal(4, decisionDto.Players.Single(player => player.PlayerId == attacker.PlayerId).Energy);

        var leaveResult = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, tokyoDefender.PlayerId));
        var buyResult = engine.Execute(gameState, new BuyFaceUpCardCommand(0, attacker.PlayerId));
        var purchaseDto = gameState.ToDto();

        Assert.True(leaveResult.Success, leaveResult.Error);
        Assert.True(buyResult.Success, buyResult.Error);
        Assert.Equal(gameState.Version, purchaseDto.Version);
        Assert.Null(purchaseDto.PendingDecision);
        Assert.Equal(TurnPhase.Purchase, purchaseDto.CurrentTurn!.Phase);
        Assert.True(purchaseDto.CurrentTurn.Flags.EnteredTokyo);
        Assert.True(purchaseDto.CurrentTurn.Flags.BoughtCard);
        Assert.Equal(attacker.PlayerId, purchaseDto.Tokyo.CityOccupantId);
        Assert.Equal(TokyoSlot.City, purchaseDto.Players.Single(player => player.PlayerId == attacker.PlayerId).TokyoSlot);
        Assert.Equal(TokyoSlot.None, purchaseDto.Players.Single(player => player.PlayerId == tokyoDefender.PlayerId).TokyoSlot);
        Assert.Equal(1, purchaseDto.Players.Single(player => player.PlayerId == attacker.PlayerId).Energy);
        Assert.Equal(2, purchaseDto.Players.Single(player => player.PlayerId == attacker.PlayerId).VictoryPoints);
        Assert.DoesNotContain(purchaseDto.Market.FaceUpCards, card => card?.CardId == KnownCardIds.CornerStore);
        Assert.Equal(1, purchaseDto.Market.DiscardPileCount);
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
