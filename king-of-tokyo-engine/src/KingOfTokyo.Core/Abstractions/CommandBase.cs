namespace KingOfTokyo.Core.Abstractions;

public abstract class CommandBase : IGameCommand
{
    public int? ActorPlayerId { get; }

    protected CommandBase(int? actorPlayerId = null)
    {
        ActorPlayerId = actorPlayerId;
    }
}