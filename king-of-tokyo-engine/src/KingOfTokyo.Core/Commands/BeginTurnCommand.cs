using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BeginTurnCommand : CommandBase
{
    public BeginTurnCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}