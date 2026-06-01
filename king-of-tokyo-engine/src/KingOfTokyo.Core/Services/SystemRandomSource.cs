using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Services;

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource() : this(Random.Shared)
    {
    }

    public SystemRandomSource(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public DieFace RollDieFace()
    {
        var value = _random.Next(1, 7);
        return (DieFace)value;
    }
}