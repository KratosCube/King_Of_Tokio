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

public sealed class TwoPlayerFullTurnLifecycleFlowTests
{
    [Fact]
    public void TurnFlow_Should_HandleTokyoEntryEndTurnAndNextPlayerTurn_InTwoPlayerGame()
    {
        var gameState = CreateGameState(2);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        var initializeResult = engine.Execute(gameState, new InitializeGameCommand());
        var firstPlayer = gameState.GetCurrentPlayer();
        var beginFirstTurnResult = engine.Execute(gameState, new BeginTurnCommand(firstPlayer.PlayerId));
        var rollResult = engine.Execute(gameState, new RollDiceCommand(firstPlayer.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(firstPlayer.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(firstPlayer.PlayerId));
        var advanceResult = engine.Execute(gameState, new AdvanceToNextPlayerCommand());
        var secondPlayer = gameState.GetCurrentPlayer();
        var beginSecondTurnResult = engine.Execute(gameState, new BeginTurnCommand(secondPlayer.PlayerId));

        Assert.True(initializeResult.Success, initializeResult.Error);
        Assert.True(beginFirstTurnResult.Success, beginFirstTurnResult.Error);
        Assert.True(rollResult.Success, rollResult.Error);
        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.True(advanceResult.Success, advanceResult.Error);
        Assert.True(beginSecondTurnResult.Success, beginSecondTurnResult.Error);

        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Equal(TokyoSlot.City, firstPlayer.TokyoSlot);
        Assert.Equal(firstPlayer.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.Equal(1, firstPlayer.VictoryPoints);
        Assert.Equal(1, firstPlayer.Energy);
        Assert.NotEqual(firstPlayer.PlayerId, secondPlayer.PlayerId);
        Assert.Equal(secondPlayer.PlayerId, gameState.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, gameState.CurrentTurn.Phase);
        Assert.Equal(GameStatus.Running, gameState.Status);
        Assert.Null(gameState.WinnerInfo);
        Assert.Contains(finalizeResult.NewEvents, e => e is TokyoEnteredEvent entered &&
                                                    entered.PlayerId == firstPlayer.PlayerId &&
                                                    entered.Slot == TokyoSlot.City);
        Assert.Contains(finalizeResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.PlayerId == firstPlayer.PlayerId &&
                                                    gained.Amount == 1 &&
                                                    gained.Reason == "Entered Tokyo.");
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
