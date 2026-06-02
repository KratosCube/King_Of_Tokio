using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Rules.Attack;

public sealed class AttackResolver
{
    private readonly KeepCardRulesService _keepCardRulesService;

    public AttackResolver(KeepCardRulesService? keepCardRulesService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public IReadOnlyList<DamagePacket> ResolveAttack(
        GameState gameState,
        PlayerState attacker,
        DiceResolutionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(summary);

        if (!attacker.IsAlive || summary.AttackCount <= 0)
        {
            return Array.Empty<DamagePacket>();
        }

        var totalAttackDamage = summary.AttackCount +
                                _keepCardRulesService.GetBonusAttackDamage(attacker, summary.AttackCount);

        IEnumerable<PlayerState> targets = _keepCardRulesService.HasNovaBreath(attacker)
            ? gameState.Players.Where(p => p.IsAlive && p.PlayerId != attacker.PlayerId)
            : attacker.TokyoSlot == TokyoSlot.None
                ? gameState.Players.Where(p =>
                    p.IsAlive &&
                    p.PlayerId != attacker.PlayerId &&
                    p.TokyoSlot != TokyoSlot.None)
                : gameState.Players.Where(p =>
                    p.IsAlive &&
                    p.PlayerId != attacker.PlayerId &&
                    p.TokyoSlot == TokyoSlot.None);

        var neighboringPlayerIds = GetAliveNeighborPlayerIds(gameState, attacker.PlayerId);
        var hasFireBreathing = attacker.HasKeepCard(KnownCardIds.FireBreathing);

        return targets
            .Select(target => new DamagePacket
            {
                SourcePlayerId = attacker.PlayerId,
                TargetPlayerId = target.PlayerId,
                Amount = totalAttackDamage + (hasFireBreathing && neighboringPlayerIds.Contains(target.PlayerId) ? 1 : 0),
                DamageKind = DamageKind.Attack,
                CountsAsAttack = true,
                AllowsTokyoLeave = attacker.TokyoSlot == TokyoSlot.None && target.TokyoSlot != TokyoSlot.None
            })
            .ToArray();
    }

    private static IReadOnlySet<int> GetAliveNeighborPlayerIds(GameState gameState, int playerId)
    {
        var alivePlayers = gameState.Players
            .Where(player => player.IsAlive)
            .OrderBy(player => player.PlayerId)
            .ToArray();

        if (alivePlayers.Length <= 1)
        {
            return new HashSet<int>();
        }

        var playerIndex = Array.FindIndex(alivePlayers, player => player.PlayerId == playerId);
        if (playerIndex < 0)
        {
            return new HashSet<int>();
        }

        var previousPlayerId = alivePlayers[(playerIndex - 1 + alivePlayers.Length) % alivePlayers.Length].PlayerId;
        var nextPlayerId = alivePlayers[(playerIndex + 1) % alivePlayers.Length].PlayerId;

        return new HashSet<int> { previousPlayerId, nextPlayerId };
    }
}
