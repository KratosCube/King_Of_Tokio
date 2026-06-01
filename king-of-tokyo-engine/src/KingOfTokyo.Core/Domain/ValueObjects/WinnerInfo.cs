namespace KingOfTokyo.Core.Domain.ValueObjects;

public sealed record WinnerInfo
{
    public int? WinnerPlayerId { get; init; }
    public bool HasWinner => WinnerPlayerId.HasValue;
    public string? Reason { get; init; }

    public WinnerInfo(int? winnerPlayerId, string? reason)
    {
        WinnerPlayerId = winnerPlayerId;
        Reason = reason;
    }

    public static WinnerInfo NoWinner(string? reason = null) => new(null, reason);
    public static WinnerInfo Winner(int playerId, string reason) => new(playerId, reason);
}