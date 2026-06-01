using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;

namespace KingOfTokyo.Core.Engine;

public sealed class EngineStepResult
{
    public IReadOnlyList<GameEventBase> Events { get; }
    public PendingDecision? PendingDecision { get; }

    public static EngineStepResult Empty { get; } =
        new(Array.Empty<GameEventBase>(), null);

    public EngineStepResult(
        IReadOnlyList<GameEventBase>? events = null,
        PendingDecision? pendingDecision = null)
    {
        Events = events ?? Array.Empty<GameEventBase>();
        PendingDecision = pendingDecision;
    }
}