using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyOpportunistRevealedCardCommand : CommandBase
{
    public BuyOpportunistRevealedCardCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}
