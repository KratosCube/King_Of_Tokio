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
            VictoryMode.FirstToTwentyPoints => ResolveFirstToTargetPoints(gameState, alivePlayers),
            VictoryMode.LastMonsterStanding => ResolveLastMonsterStanding(alivePlayers),
            _ => ResolveStandard(gameState, alivePlayers)
        };
    }

    private static WinnerInfo? ResolveStandard(GameState gameState, IReadOnlyList<PlayerState> alivePlayers)
    {
        if (ResolveTargetPointWinner(gameState, alivePlayers) is { } targetPointWinner)
        {
            return targetPointWinner;
        }

        if (alivePlayers.Count == 1)
        {
            return WinnerInfo.Winner(alivePlayers[0].PlayerId, "Last monster standing.");
        }

        return null;
    }

    private static WinnerInfo? ResolveFirstToTargetPoints(GameState gameState, IReadOnlyList<PlayerState> alivePlayers)
    {
        return ResolveTargetPointWinner(gameState, alivePlayers);
    }

    private static WinnerInfo? ResolveTargetPointWinner(GameState gameState, IReadOnlyList<PlayerState> alivePlayers)
    {
        var currentPlayer = gameState.GetCurrentPlayer();
        var targetVictoryPoints = gameState.Options.TargetVictoryPoints;

        if (currentPlayer.IsAlive && currentPlayer.VictoryPoints >= targetVictoryPoints)
        {
            return WinnerInfo.Winner(currentPlayer.PlayerId, $"Reached {targetVictoryPoints} victory points.");
        }

        var firstAlivePlayerAtTarget = alivePlayers.FirstOrDefault(player => player.VictoryPoints >= targetVictoryPoints);
        return firstAlivePlayerAtTarget is null
            ? null
            : WinnerInfo.Winner(firstAlivePlayerAtTarget.PlayerId, $"Reached {targetVictoryPoints} victory points.");
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
