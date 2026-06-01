using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class EndTurnCommand : CommandBase
{
    public EndTurnCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}