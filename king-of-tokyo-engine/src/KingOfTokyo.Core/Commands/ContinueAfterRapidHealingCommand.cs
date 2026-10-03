using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ContinueAfterRapidHealingCommand : CommandBase
{
    public ContinueAfterRapidHealingCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}
