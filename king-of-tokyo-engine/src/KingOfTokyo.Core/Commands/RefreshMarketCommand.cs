using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class RefreshMarketCommand : CommandBase
{
    public RefreshMarketCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}