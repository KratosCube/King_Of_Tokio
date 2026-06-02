using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardHealing;

public sealed class HealingRayServiceTests
{
    [Fact]
    public void HealOtherPlayer_Should_HealTargetAndPayHealerTwoEnergyPerHealedDamage()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        target.TakeDamage(3);
        target.GainEnergy(6);
        var service = new HealingRayService();

        var result = service.HealOtherPlayer(healer, target, requestedHealing: 3);

        Assert.Equal(3, result.HealedAmount);
        Assert.Equal(6, result.EnergyPaid);
        Assert.Equal(10, target.Health);
        Assert.Equal(0, target.Energy);
        Assert.Equal(6, healer.Energy);
    }

    [Fact]
    public void HealOtherPlayer_Should_PayRemainingEnergy_WhenTargetCannotPayFullAmount()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        target.TakeDamage(3);
        target.GainEnergy(3);
        var service = new HealingRayService();

        var result = service.HealOtherPlayer(healer, target, requestedHealing: 3);

        Assert.Equal(3, result.HealedAmount);
        Assert.Equal(3, result.EnergyPaid);
        Assert.Equal(10, target.Health);
        Assert.Equal(0, target.Energy);
        Assert.Equal(3, healer.Energy);
    }

    [Fact]
    public void HealOtherPlayer_Should_OnlyChargeForActualHealing()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        target.TakeDamage(1);
        target.GainEnergy(6);
        var service = new HealingRayService();

        var result = service.HealOtherPlayer(healer, target, requestedHealing: 3);

        Assert.Equal(1, result.HealedAmount);
        Assert.Equal(2, result.EnergyPaid);
        Assert.Equal(10, target.Health);
        Assert.Equal(4, target.Energy);
        Assert.Equal(2, healer.Energy);
    }

    [Fact]
    public void HealOtherPlayer_Should_NotCharge_WhenTargetIsAlreadyAtFullHealth()
    {
        var healer = new PlayerState(0, "Healer");
        var target = new PlayerState(1, "Target");
        target.GainEnergy(6);
        var service = new HealingRayService();

        var result = service.HealOtherPlayer(healer, target, requestedHealing: 3);

        Assert.Equal(0, result.HealedAmount);
        Assert.Equal(0, result.EnergyPaid);
        Assert.Equal(10, target.Health);
        Assert.Equal(6, target.Energy);
        Assert.Equal(0, healer.Energy);
    }

    [Fact]
    public void HealOtherPlayer_Should_Throw_WhenHealingSelf()
    {
        var player = new PlayerState(0, "Monster");
        player.TakeDamage(2);
        var service = new HealingRayService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.HealOtherPlayer(player, player, requestedHealing: 2));
        Assert.Equal("Healing Ray can only heal other players.", exception.Message);
    }
}
