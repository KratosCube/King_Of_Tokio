namespace KingOfTokyo.Core.Commands;

public sealed class ActivateMimickedRapidHealingCommand
{
    public int? ActorPlayerId { get; }

    public ActivateMimickedRapidHealingCommand(int? actorPlayerId = null)
    {
        ActorPlayerId = actorPlayerId;
    }
}
