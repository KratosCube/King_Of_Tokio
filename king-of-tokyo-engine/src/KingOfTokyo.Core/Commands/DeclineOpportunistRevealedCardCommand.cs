using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class DeclineOpportunistRevealedCardCommand : CommandBase
{
    public DeclineOpportunistRevealedCardCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}
