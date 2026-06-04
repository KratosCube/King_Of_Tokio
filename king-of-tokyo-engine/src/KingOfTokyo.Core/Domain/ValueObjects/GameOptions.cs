using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record GameOptions
{
    public const int DefaultInitialHealth = 10;
    public const int DefaultTargetVictoryPoints = 20;

    public int PlayerCount { get; init; }
    public bool UseBay { get; init; }
    public VictoryMode VictoryMode { get; init; }
    public int InitialHealth { get; init; }
    public int TargetVictoryPoints { get; init; }

    public GameOptions(
        int playerCount,
        VictoryMode victoryMode = VictoryMode.Standard,
        int initialHealth = DefaultInitialHealth,
        int targetVictoryPoints = DefaultTargetVictoryPoints)
    {
        if (playerCount is < 2 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Player count must be between 2 and 6.");
        }

        if (initialHealth is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(initialHealth), "Initial health must be between 1 and 50.");
        }

        if (targetVictoryPoints is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(targetVictoryPoints), "Target victory points must be between 1 and 100.");
        }

        PlayerCount = playerCount;
        UseBay = playerCount >= 5;
        VictoryMode = victoryMode;
        InitialHealth = initialHealth;
        TargetVictoryPoints = targetVictoryPoints;
    }
}
