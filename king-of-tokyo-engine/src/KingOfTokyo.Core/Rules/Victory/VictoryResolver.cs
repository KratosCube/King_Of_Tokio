using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Rules.Victory;

public sealed class VictoryResolver
{
    public WinnerInfo? Resolve(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var alivePlayers = gameState.GetAlivePlayers();

        if (alivePlayers.Count == 0)
        {
            return WinnerInfo.NoWinner("All monsters were eliminated.");
        }

        return gameState.Options.VictoryMode switch
        {
            VictoryMode.FirstToTwentyPoints => ResolveFirstToTwenty(gameState),
            VictoryMode.LastMonsterStanding => ResolveLastMonsterStanding(alivePlayers),
            _ => ResolveStandard(gameState, alivePlayers)
        };
    }

    private static WinnerInfo? ResolveStandard(GameState gameState, IReadOnlyList<PlayerState> alivePlayers)
    {
        var currentPlayer = gameState.GetCurrentPlayer();

        if (currentPlayer.IsAlive && currentPlayer.VictoryPoints >= 20)
        {
            return WinnerInfo.Winner(currentPlayer.PlayerId, "Reached 20 victory points.");
        }

        if (alivePlayers.Count == 1)
        {
            return WinnerInfo.Winner(alivePlayers[0].PlayerId, "Last monster standing.");
        }

        return null;
    }

    private static WinnerInfo? ResolveFirstToTwenty(GameState gameState)
    {
        var currentPlayer = gameState.GetCurrentPlayer();

        if (currentPlayer.IsAlive && currentPlayer.VictoryPoints >= 20)
        {
            return WinnerInfo.Winner(currentPlayer.PlayerId, "Reached 20 victory points.");
        }

        return null;
    }

    private static WinnerInfo? ResolveLastMonsterStanding(IReadOnlyList<PlayerState> alivePlayers)
    {
        if (alivePlayers.Count == 1)
        {
            return WinnerInfo.Winner(alivePlayers[0].PlayerId, "Last monster standing.");
        }

        return null;
    }
}