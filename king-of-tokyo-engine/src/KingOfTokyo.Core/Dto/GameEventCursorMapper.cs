using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Dto;

public static class GameEventCursorMapper
{
    public static GameEventCursorDto MapEventsSince(GameState gameState, long fromVersionExclusive)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (fromVersionExclusive < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromVersionExclusive));
        }

        if (fromVersionExclusive > gameState.Version)
        {
            throw new InvalidOperationException("Requested event cursor is ahead of the current game version.");
        }

        var skippedEventCount = (int)Math.Min(fromVersionExclusive, gameState.EventLog.Count);
        var events = gameState.EventLog
            .Skip(skippedEventCount)
            .Select((gameEvent, index) => new GameEventEnvelopeDto(
                fromVersionExclusive + index + 1,
                gameEvent))
            .ToArray();

        return new GameEventCursorDto(
            gameState.GameId,
            fromVersionExclusive,
            gameState.Version,
            events);
    }
}
