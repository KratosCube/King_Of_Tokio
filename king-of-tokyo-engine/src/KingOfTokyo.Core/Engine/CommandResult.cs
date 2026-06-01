using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Engine;

public sealed class CommandResult
{
    public GameState GameState { get; }
    public IReadOnlyList<GameEventBase> NewEvents { get; }
    public PendingDecision? PendingDecision { get; }
    public bool Success { get; }
    public string? Error { get; }

    private CommandResult(
        GameState gameState,
        IReadOnlyList<GameEventBase> newEvents,
        PendingDecision? pendingDecision,
        bool success,
        string? error)
    {
        GameState = gameState;
        NewEvents = newEvents;
        PendingDecision = pendingDecision;
        Success = success;
        Error = error;
    }

    public static CommandResult Successful(
        GameState gameState,
        IReadOnlyList<GameEventBase>? newEvents = null,
        PendingDecision? pendingDecision = null)
    {
        return new CommandResult(
            gameState,
            newEvents ?? Array.Empty<GameEventBase>(),
            pendingDecision,
            true,
            null);
    }

    public static CommandResult Failed(GameState gameState, string error)
    {
        return new CommandResult(
            gameState,
            Array.Empty<GameEventBase>(),
            null,
            false,
            error);
    }
}