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

        IEnumerable<PlayerState> targets =
            attacker.TokyoSlot == TokyoSlot.None
                ? gameState.Players.Where(p =>
                    p.IsAlive &&
                    p.PlayerId != attacker.PlayerId &&
                    p.TokyoSlot != TokyoSlot.None)
                : gameState.Players.Where(p =>
                    p.IsAlive &&
                    p.PlayerId != attacker.PlayerId &&
                    p.TokyoSlot == TokyoSlot.None);

        return targets
            .Select(target => new DamagePacket
            {
                SourcePlayerId = attacker.PlayerId,
                TargetPlayerId = target.PlayerId,
                Amount = totalAttackDamage,
                DamageKind = DamageKind.Attack,
                CountsAsAttack = true,
                AllowsTokyoLeave = attacker.TokyoSlot == TokyoSlot.None && target.TokyoSlot != TokyoSlot.None
            })
            .ToArray();
    }
}