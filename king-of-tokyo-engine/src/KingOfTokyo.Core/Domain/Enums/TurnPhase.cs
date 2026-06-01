namespace KingOfTokyo.Core.Domain.Enums;

public enum TurnPhase
{
    NotStarted = 0,
    TurnStart = 1,
    Rolling = 2,
    DiceResolved = 3,
    Purchase = 4,
    TurnEnd = 5,
    Finished = 6
}