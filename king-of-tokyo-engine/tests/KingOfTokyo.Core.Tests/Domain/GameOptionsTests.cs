using KingOfTokyo.Core.Domain.ValueObjects;
using Xunit;

namespace KingOfTokyo.Core.Tests.Domain;

public sealed class GameOptionsTests
{
    [Fact]
    public void Constructor_Should_AllowTwoPlayersWithoutBay()
    {
        var options = new GameOptions(2);

        Assert.Equal(2, options.PlayerCount);
        Assert.False(options.UseBay);
    }

    [Fact]
    public void Constructor_Should_RejectSinglePlayer()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GameOptions(1));

        Assert.Equal("playerCount", exception.ParamName);
        Assert.Contains("between 2 and 6", exception.Message);
    }
}
