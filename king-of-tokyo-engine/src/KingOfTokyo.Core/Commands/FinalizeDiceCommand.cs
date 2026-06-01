using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class FinalizeDiceCommand : CommandBase
{
    public FinalizeDiceCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}