using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class FinalizeDiceCommand : CommandBase
{
    public int HeartsReservedForHealingRay { get; }

    public FinalizeDiceCommand(int? actorPlayerId = null, int heartsReservedForHealingRay = 0) : base(actorPlayerId)
    {
        HeartsReservedForHealingRay = heartsReservedForHealingRay;
    }
}
