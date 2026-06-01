using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateWingsCommand : CommandBase
{
    public ActivateWingsCommand(int actorPlayerId) : base(actorPlayerId)
    {
    }
}
