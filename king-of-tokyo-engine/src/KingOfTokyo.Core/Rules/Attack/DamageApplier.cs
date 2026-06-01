using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Rules.Attack;

public sealed class DamageApplier
{
    private readonly KeepCardRulesService _keepCardRulesService;

    public DamageApplier(KeepCardRulesService? keepCardRulesService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public int ApplyDamage(PlayerState target, DamagePacket packet)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(packet);

        var ignoredDamage = _keepCardRulesService.GetIgnoredDamage(target, packet.DamageKind);
        var finalDamage = Math.Max(0, packet.Amount - ignoredDamage);

        var healthBefore = target.Health;
        target.TakeDamage(finalDamage);

        return healthBefore - target.Health;
    }
}