using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Dto;

public sealed record GameEventCursorDto(
    Guid GameId,
    long FromVersionExclusive,
    long CurrentVersion,
    IReadOnlyList<GameEventEnvelopeDto> Events);

public sealed record GameEventEnvelopeDto(
    long Version,
    GameEventBase Event);
