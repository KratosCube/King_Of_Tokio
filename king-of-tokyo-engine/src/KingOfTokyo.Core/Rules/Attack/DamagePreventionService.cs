using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Rules.Attack;

public sealed class DamagePreventionService
{
    private readonly KeepCardRulesService _keepCardRulesService;

    public DamagePreventionService(KeepCardRulesService? keepCardRulesService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public DamagePreventionResult Resolve(PlayerState target, DamagePacket packet)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(packet);

        var originalAmount = Math.Max(0, packet.Amount);
        var preventableDamage = Math.Max(0, _keepCardRulesService.GetIgnoredDamage(target, packet.DamageKind));
        var preventedDamage = Math.Min(originalAmount, preventableDamage);
        var finalAmount = originalAmount - preventedDamage;

        return new DamagePreventionResult(originalAmount, preventedDamage, finalAmount);
    }
}

public sealed record DamagePreventionResult(
    int OriginalAmount,
    int PreventedDamage,
    int FinalAmount);
