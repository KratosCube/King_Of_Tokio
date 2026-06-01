using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class DeclinePeekedTopDeckCardCommand : CommandBase
{
    public DeclinePeekedTopDeckCardCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}