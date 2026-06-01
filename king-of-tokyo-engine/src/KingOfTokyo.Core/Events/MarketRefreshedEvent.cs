using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Events;

public sealed class MarketRefreshedEvent : GameEventBase
{
    public int PlayerId { get; }
    public int CostSpent { get; }
    public IReadOnlyList<string> RefreshedCardIds { get; }

    public MarketRefreshedEvent(int playerId, int costSpent, IReadOnlyList<string> refreshedCardIds)
    {
        if (costSpent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costSpent));
        }

        PlayerId = playerId;
        CostSpent = costSpent;
        RefreshedCardIds = refreshedCardIds ?? throw new ArgumentNullException(nameof(refreshedCardIds));
    }

    public override string EventName => nameof(MarketRefreshedEvent);
}