using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class MimicTargetCleanupService
{
    public int ClearTargetsForLostCard(GameState gameState, int originalOwnerPlayerId, string lostCardId)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (originalOwnerPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalOwnerPlayerId));
        }

        if (string.IsNullOrWhiteSpace(lostCardId))
        {
            throw new ArgumentException("Lost card id must not be empty.", nameof(lostCardId));
        }

        return ClearMatchingTargets(
            gameState,
            target => target.OwnerPlayerId == originalOwnerPlayerId && target.CardId == lostCardId);
    }

    public int ClearTargetsForOwner(GameState gameState, int originalOwnerPlayerId)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        if (originalOwnerPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalOwnerPlayerId));
        }

        return ClearMatchingTargets(
            gameState,
            target => target.OwnerPlayerId == originalOwnerPlayerId);
    }

    private static int ClearMatchingTargets(GameState gameState, Func<MimicTargetState, bool> predicate)
    {
        var clearedCount = 0;

        foreach (var player in gameState.Players)
        {
            foreach (var mimic in player.KeepCards.Where(card => card.CardId == KnownCardIds.Mimic))
            {
                if (mimic.MimicTarget is null || !predicate(mimic.MimicTarget))
                {
                    continue;
                }

                mimic.ClearMimicTarget();
                clearedCount++;
            }
        }

        return clearedCount;
    }
}
