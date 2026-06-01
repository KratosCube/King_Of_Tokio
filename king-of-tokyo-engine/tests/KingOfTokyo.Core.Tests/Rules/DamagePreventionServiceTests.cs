using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Attack;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class DamagePreventionServiceTests
{
    [Fact]
    public void Resolve_Should_NotPreventDamage_WhenPlayerHasNoPreventionCards()
    {
        var target = new PlayerState(0, "Monster");
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(0, result.PreventedDamage);
        Assert.Equal(3, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_PreventOneDamage_WhenPlayerHasArmorPlating()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(1, result.PreventedDamage);
        Assert.Equal(2, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_NotPreventBelowZero_WhenPreventionExceedsDamage()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 1));

        Assert.Equal(1, result.OriginalAmount);
        Assert.Equal(1, result.PreventedDamage);
        Assert.Equal(0, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_TreatNegativeDamageAsZero()
    {
        var target = new PlayerState(0, "Monster");
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: -1));

        Assert.Equal(0, result.OriginalAmount);
        Assert.Equal(0, result.PreventedDamage);
        Assert.Equal(0, result.FinalAmount);
    }

    private static DamagePacket CreateDamagePacket(int amount)
    {
        return new DamagePacket
        {
            SourcePlayerId = 1,
            TargetPlayerId = 0,
            Amount = amount,
            DamageKind = DamageKind.Attack,
            CountsAsAttack = true,
            AllowsTokyoLeave = true
        };
    }
}
