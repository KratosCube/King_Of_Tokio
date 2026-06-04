using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Dto;

public static class GameEventCursorMapper
{
    public static GameEventCursorDto MapEventsSince(GameState gameState, long fromEventSequenceExclusive)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (fromEventSequenceExclusive < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromEventSequenceExclusive));
        }

        if (fromEventSequenceExclusive > gameState.EventLog.Count)
        {
            throw new InvalidOperationException("Requested event cursor is ahead of the current event sequence.");
        }

        var skippedEventCount = (int)fromEventSequenceExclusive;
        var events = gameState.EventLog
            .Skip(skippedEventCount)
            .Select((gameEvent, index) => new GameEventEnvelopeDto(
                fromEventSequenceExclusive + index + 1,
                gameEvent))
            .ToArray();

        return new GameEventCursorDto(
            gameState.GameId,
            fromEventSequenceExclusive,
            gameState.EventLog.Count,
            gameState.Version,
            events);
    }
}
