using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record GameOptions
{
    public int PlayerCount { get; init; }
    public bool UseBay { get; init; }
    public VictoryMode VictoryMode { get; init; }

    public GameOptions(int playerCount, VictoryMode victoryMode = VictoryMode.Standard)
    {
        if (playerCount is < 3 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Player count must be between 3 and 6.");
        }

        PlayerCount = playerCount;
        UseBay = playerCount >= 5;
        VictoryMode = victoryMode;
    }
}