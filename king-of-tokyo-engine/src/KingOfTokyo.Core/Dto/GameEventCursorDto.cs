using System.Text.Json;

namespace KingOfTokyo.Core.Dto;

public sealed record GameEventCursorDto(
    Guid GameId,
    long FromEventSequenceExclusive,
    long CurrentEventSequence,
    long CurrentGameVersion,
    IReadOnlyList<GameEventEnvelopeDto> Events);

public sealed record GameEventEnvelopeDto(
    long EventSequence,
    JsonElement Event);
