using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateSmokeCloudCommand : CommandBase
{
    public ActivateSmokeCloudCommand(int actorPlayerId) : base(actorPlayerId)
    {
    }
}
