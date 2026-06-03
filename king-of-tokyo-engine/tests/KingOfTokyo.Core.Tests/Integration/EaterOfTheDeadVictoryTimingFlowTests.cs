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

public sealed class EaterOfTheDeadVictoryTimingFlowTests
{
    [Fact]
    public void EndTurn_Should_EndGame_WhenEaterOfTheDeadOwnerReachesTwentyDuringAnotherPlayersTurn()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);
        var observer = gameState.GetPlayerById(2);
        defender.TakeDamage(9);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        observer.GainVictoryPoints(17);
        observer.AddKeepCard(CreateKeepCard(KnownCardIds.EaterOfTheDead, "Eater of the Dead", 4));

        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        var finalizeResult = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));
        var endTurnResult = engine.Execute(gameState, new EndTurnCommand(attacker.PlayerId));

        Assert.True(finalizeResult.Success, finalizeResult.Error);
        Assert.True(endTurnResult.Success, endTurnResult.Error);
        Assert.False(defender.IsAlive);
        Assert.True(observer.IsAlive);
        Assert.Equal(20, observer.VictoryPoints);
        Assert.Equal(GameStatus.Finished, gameState.Status);
        Assert.NotNull(gameState.WinnerInfo);
        Assert.True(gameState.WinnerInfo!.HasWinner);
        Assert.Equal(observer.PlayerId, gameState.WinnerInfo.WinnerPlayerId);
        Assert.Equal("Reached 20 victory points.", gameState.WinnerInfo.Reason);
        Assert.Contains(finalizeResult.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                                    gained.PlayerId == observer.PlayerId &&
                                                    gained.Amount == 3 &&
                                                    gained.Reason == "Keep card: Eater of the Dead.");
        Assert.Contains(endTurnResult.NewEvents, e => e is GameEndedEvent ended &&
                                                    ended.WinnerPlayerId == observer.PlayerId &&
                                                    ended.Reason == "Reached 20 victory points.");
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

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
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
