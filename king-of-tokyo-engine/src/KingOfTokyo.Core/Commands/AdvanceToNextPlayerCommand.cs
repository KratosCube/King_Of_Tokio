using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class AdvanceToNextPlayerCommand : CommandBase
{
    public AdvanceToNextPlayerCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}