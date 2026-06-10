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

public sealed class RepresentativeGameFlowTests
{
    [Fact]
    public void RepresentativeGameFlow_Should_PlayMultipleTurnsThroughTokyoDecisionsPurchaseAndStartTokyoScoring()
    {
        var gameState = CreateGameState(3);
        var player0 = gameState.GetPlayerById(0);
        var player1 = gameState.GetPlayerById(1);
        var player2 = gameState.GetPlayerById(2);
        var engine = CreateEngine(
            // Player 0: attacks into empty Tokyo, gains 3 energy, then buys Heal.
            DieFace.Attack, DieFace.Energy, DieFace.Energy,
            DieFace.Energy, DieFace.One, DieFace.Two,
            // Player 1: scores 2 VP from three 2s, attacks player 0 in Tokyo, and takes Tokyo after player 0 leaves.
            DieFace.Attack, DieFace.Attack, DieFace.Two,
            DieFace.Two, DieFace.Two, DieFace.Energy,
            // Player 2: attacks player 1 in Tokyo; player 1 stays.
            DieFace.Attack, DieFace.Heart, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three,
            // Player 0: heals back outside Tokyo and scores 1s.
            DieFace.Heart, DieFace.Heart, DieFace.Energy,
            DieFace.One, DieFace.One, DieFace.One);

        ExecuteSuccessful(engine, gameState, new InitializeGameCommand());

        ExecuteSuccessful(engine, gameState, new BeginTurnCommand(player0.PlayerId));
        ExecuteSuccessful(engine, gameState, new RollDiceCommand(player0.PlayerId));
        var player0Finalize = ExecuteSuccessful(engine, gameState, new FinalizeDiceCommand(player0.PlayerId));

        Assert.Null(player0Finalize.PendingDecision);
        Assert.Equal(TokyoSlot.City, player0.TokyoSlot);
        Assert.Equal(player0.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(1, player0.VictoryPoints);
        Assert.Equal(3, player0.Energy);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);

        var player0Buy = ExecuteSuccessful(engine, gameState, new BuyFaceUpCardCommand(1, player0.PlayerId));
        Assert.Equal(0, player0.Energy);
        Assert.Contains(player0Buy.NewEvents, e => e is CardBoughtEvent bought &&
                                                   bought.PlayerId == player0.PlayerId &&
                                                   bought.CardId == KnownCardIds.Heal);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.Heal);

        ExecuteSuccessful(engine, gameState, new EndTurnCommand(player0.PlayerId));
        ExecuteSuccessful(engine, gameState, new AdvanceToNextPlayerCommand());
        Assert.Equal(player1.PlayerId, gameState.GetCurrentPlayer().PlayerId);

        ExecuteSuccessful(engine, gameState, new BeginTurnCommand(player1.PlayerId));
        ExecuteSuccessful(engine, gameState, new RollDiceCommand(player1.PlayerId));
        var player1Finalize = ExecuteSuccessful(engine, gameState, new FinalizeDiceCommand(player1.PlayerId));

        Assert.NotNull(player1Finalize.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, player1Finalize.PendingDecision!.DecisionType);
        Assert.Equal(player0.PlayerId, player1Finalize.PendingDecision.PlayerId);
        Assert.Equal(8, player0.Health);
        Assert.Equal(2, player1.VictoryPoints);
        Assert.Equal(1, player1.Energy);

        var player0LeavesTokyo = ExecuteSuccessful(engine, gameState, new ChooseLeaveTokyoCommand(true, player0.PlayerId));
        Assert.Null(player0LeavesTokyo.PendingDecision);
        Assert.Equal(TokyoSlot.None, player0.TokyoSlot);
        Assert.Equal(TokyoSlot.City, player1.TokyoSlot);
        Assert.Equal(player1.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(3, player1.VictoryPoints);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);
        Assert.Contains(player0LeavesTokyo.NewEvents, e => e is TokyoLeftEvent left && left.PlayerId == player0.PlayerId);
        Assert.Contains(player0LeavesTokyo.NewEvents, e => e is TokyoEnteredEvent entered && entered.PlayerId == player1.PlayerId);

        ExecuteSuccessful(engine, gameState, new EndTurnCommand(player1.PlayerId));
        ExecuteSuccessful(engine, gameState, new AdvanceToNextPlayerCommand());
        Assert.Equal(player2.PlayerId, gameState.GetCurrentPlayer().PlayerId);

        ExecuteSuccessful(engine, gameState, new BeginTurnCommand(player2.PlayerId));
        ExecuteSuccessful(engine, gameState, new RollDiceCommand(player2.PlayerId));
        var player2Finalize = ExecuteSuccessful(engine, gameState, new FinalizeDiceCommand(player2.PlayerId));

        Assert.NotNull(player2Finalize.PendingDecision);
        Assert.Equal(DecisionType.LeaveTokyo, player2Finalize.PendingDecision!.DecisionType);
        Assert.Equal(player1.PlayerId, player2Finalize.PendingDecision.PlayerId);
        Assert.Equal(9, player1.Health);

        var player1StaysInTokyo = ExecuteSuccessful(engine, gameState, new ChooseLeaveTokyoCommand(false, player1.PlayerId));
        Assert.Null(player1StaysInTokyo.PendingDecision);
        Assert.Equal(TokyoSlot.City, player1.TokyoSlot);
        Assert.Equal(TokyoSlot.None, player2.TokyoSlot);
        Assert.Equal(player1.PlayerId, gameState.Tokyo.CityOccupantId);

        ExecuteSuccessful(engine, gameState, new EndTurnCommand(player2.PlayerId));
        ExecuteSuccessful(engine, gameState, new AdvanceToNextPlayerCommand());
        Assert.Equal(player0.PlayerId, gameState.GetCurrentPlayer().PlayerId);

        ExecuteSuccessful(engine, gameState, new BeginTurnCommand(player0.PlayerId));
        ExecuteSuccessful(engine, gameState, new RollDiceCommand(player0.PlayerId));
        ExecuteSuccessful(engine, gameState, new FinalizeDiceCommand(player0.PlayerId));

        Assert.Equal(10, player0.Health);
        Assert.Equal(2, player0.VictoryPoints);
        Assert.Equal(1, player0.Energy);
        Assert.Equal(TurnPhase.Purchase, gameState.CurrentTurn!.Phase);

        ExecuteSuccessful(engine, gameState, new EndTurnCommand(player0.PlayerId));
        ExecuteSuccessful(engine, gameState, new AdvanceToNextPlayerCommand());
        Assert.Equal(player1.PlayerId, gameState.GetCurrentPlayer().PlayerId);

        ExecuteSuccessful(engine, gameState, new BeginTurnCommand(player1.PlayerId));

        Assert.Equal(5, player1.VictoryPoints);
        Assert.True(gameState.CurrentTurn!.Flags.StartedTurnInTokyo);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Null(gameState.WinnerInfo);
        Assert.Equal(25, gameState.Version);
        Assert.NotEmpty(gameState.EventLog);
    }

    private static CommandResult ExecuteSuccessful(GameEngine engine, GameState gameState, IGameCommand command)
    {
        var versionBefore = gameState.Version;
        var eventLogCountBefore = gameState.EventLog.Count;

        var result = engine.Execute(gameState, command);

        Assert.True(result.Success, result.Error);
        Assert.Equal(versionBefore + 1, gameState.Version);
        Assert.Equal(eventLogCountBefore + result.NewEvents.Count, gameState.EventLog.Count);

        for (var i = 0; i < result.NewEvents.Count; i++)
        {
            Assert.Same(result.NewEvents[i], gameState.EventLog[eventLogCountBefore + i]);
        }

        return result;
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
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)),
            marketSetupService: new MarketSetupService(shuffleDeck: false));
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
