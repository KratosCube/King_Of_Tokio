namespace KingOfTokyo.Core.Abstractions;

public interface IGameCommand
{
    int? ActorPlayerId { get; }
}