using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateTelepathCommand : CommandBase
{
    public ActivateTelepathCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}