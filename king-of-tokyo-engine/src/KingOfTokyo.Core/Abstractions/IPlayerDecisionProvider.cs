using KingOfTokyo.Core.Decisions;

namespace KingOfTokyo.Core.Abstractions;

public interface IPlayerDecisionProvider
{
    object? Resolve(PendingDecision pendingDecision);
}