using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;

namespace KingOfTokyo.Core.Rules.Attack;

public sealed class DamagePreventionService
{
    private readonly KeepCardRulesService _keepCardRulesService;
    private readonly IRandomSource _randomSource;

    public DamagePreventionService(
        KeepCardRulesService? keepCardRulesService = null,
        IRandomSource? randomSource = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _randomSource = randomSource ?? new SystemRandomSource();
    }

    public DamagePreventionResult Resolve(PlayerState target, DamagePacket packet)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(packet);

        var originalAmount = Math.Max(0, packet.Amount);
        var staticPreventableDamage = Math.Max(0, _keepCardRulesService.GetIgnoredDamage(target, packet.DamageKind));
        var staticPreventedDamage = Math.Min(originalAmount, staticPreventableDamage);
        var amountAfterStaticPrevention = originalAmount - staticPreventedDamage;
        var camouflagePreventedDamage = ResolveCamouflagePrevention(target, amountAfterStaticPrevention);
        var preventedDamage = staticPreventedDamage + camouflagePreventedDamage;
        var finalAmount = originalAmount - preventedDamage;

        return new DamagePreventionResult(
            OriginalAmount: originalAmount,
            PreventedDamage: preventedDamage,
            FinalAmount: finalAmount,
            StaticPreventedDamage: staticPreventedDamage,
            CamouflagePreventedDamage: camouflagePreventedDamage);
    }

    private int ResolveCamouflagePrevention(PlayerState target, int incomingDamage)
    {
        if (incomingDamage <= 0 || !target.HasKeepCard(KnownCardIds.Camouflage))
        {
            return 0;
        }

        var preventedDamage = 0;
        for (var i = 0; i < incomingDamage; i++)
        {
            if (_randomSource.RollDieFace() == DieFace.Heart)
            {
                preventedDamage++;
            }
        }

        return preventedDamage;
    }
}

public sealed record DamagePreventionResult(
    int OriginalAmount,
    int PreventedDamage,
    int FinalAmount,
    int StaticPreventedDamage = 0,
    int CamouflagePreventedDamage = 0);