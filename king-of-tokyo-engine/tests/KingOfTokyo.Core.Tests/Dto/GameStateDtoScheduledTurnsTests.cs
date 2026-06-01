using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using Xunit;

namespace KingOfTokyo.Core.Tests.Dto;

public sealed class GameStateDtoScheduledTurnsTests
{
    [Fact]
    public void ToDto_Should_MapScheduledTurnPlayerIds()
    {
        var gameState = CreateGameState(4);
        gameState.ScheduleExtraTurn(2);
        gameState.ScheduleExtraTurn(0);

        var dto = gameState.ToDto();

        Assert.Equal(new[] { 2, 0 }, dto.ScheduledTurnPlayerIds);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }
}
