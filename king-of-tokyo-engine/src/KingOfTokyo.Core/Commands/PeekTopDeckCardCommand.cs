using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class PeekTopDeckCardCommand : CommandBase
{
    public PeekTopDeckCardCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}