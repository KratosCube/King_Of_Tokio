using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateRapidHealingCommand : CommandBase
{
    public ActivateRapidHealingCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}