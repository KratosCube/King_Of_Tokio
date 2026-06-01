using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyPeekedTopDeckCardCommand : CommandBase
{
    public BuyPeekedTopDeckCardCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}