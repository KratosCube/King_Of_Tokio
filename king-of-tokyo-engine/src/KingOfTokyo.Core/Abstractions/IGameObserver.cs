namespace KingOfTokyo.Core.Abstractions;

public interface IGameObserver
{
    void OnEvent(GameEventBase gameEvent);
}