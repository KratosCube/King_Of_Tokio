using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class RollDiceCommand : CommandBase
{
    public RollDiceCommand(int? actorPlayerId = null) : base(actorPlayerId)
    {
    }
}