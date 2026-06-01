using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Rules.Attack;

public sealed class DamageApplier
{
    private readonly KeepCardRulesService _keepCardRulesService;
    private readonly DamagePreventionService _damagePreventionService;

    public DamageApplier(
        KeepCardRulesService? keepCardRulesService = null,
        DamagePreventionService? damagePreventionService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _damagePreventionService = damagePreventionService ?? new DamagePreventionService(_keepCardRulesService);
    }

    public int ApplyDamage(PlayerState target, DamagePacket packet)
    {
        return ApplyDamage(target, packet, currentTurn: null);
    }

    public int ApplyDamage(PlayerState target, DamagePacket packet, TurnState? currentTurn)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(packet);

        var preventionResult = _damagePreventionService.Resolve(target, packet);

        var healthBefore = target.Health;
        target.TakeDamage(preventionResult.FinalAmount);

        var actualDamage = healthBefore - target.Health;
        if (actualDamage > 0)
        {
            currentTurn?.RecordDamageTaken(target.PlayerId, actualDamage);
        }

        var victoryPoints = _keepCardRulesService.GetVictoryPointsWhenTakingDamage(target, actualDamage);
        if (victoryPoints > 0)
        {
            target.GainVictoryPoints(victoryPoints);
        }

        return actualDamage;
    }
}
