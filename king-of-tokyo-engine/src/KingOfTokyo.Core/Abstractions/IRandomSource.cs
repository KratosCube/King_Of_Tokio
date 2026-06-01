using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Abstractions;

public interface IRandomSource
{
    DieFace RollDieFace();
}