using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardMimic;

public sealed class MimicKeepCardRulesServiceTests
{
    [Fact]
    public void GetExtraRerolls_Should_CountMimicCopyingGiantBrain()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.GiantBrain, "Giant Brain"));
        var service = new KeepCardRulesService();

        var extraRerolls = service.GetExtraRerolls(player);

        Assert.Equal(1, extraRerolls);
    }

    [Fact]
    public void GetExtraDiceCount_Should_CountMimicCopyingExtraHead()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.ExtraHead, "Extra Head"));
        var service = new KeepCardRulesService();

        var extraDice = service.GetExtraDiceCount(player);

        Assert.Equal(1, extraDice);
        Assert.Equal(7, service.GetEffectiveDiceCount(player));
    }

    [Theory]
    [InlineData(KnownCardIds.SpikedTail, "Spiked Tail")]
    [InlineData(KnownCardIds.AcidAttack, "Acid Attack")]
    public void GetBonusAttackDamage_Should_CountMimicCopyingFlatAttackBonusCards(string copiedCardId, string copiedCardName)
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(copiedCardId, copiedCardName));
        var service = new KeepCardRulesService();

        var bonusDamage = service.GetBonusAttackDamage(player, rolledAttackCount: 1);

        Assert.Equal(1, bonusDamage);
    }

    [Fact]
    public void GetBonusAttackDamage_Should_NotCountMimic_WhenNoTargetIsSet()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.Mimic, "Mimic", 8));
        var service = new KeepCardRulesService();

        var bonusDamage = service.GetBonusAttackDamage(player, rolledAttackCount: 1);

        Assert.Equal(0, bonusDamage);
    }

    [Fact]
    public void GetBonusAttackDamage_Should_CountOwnedCardAndMimicSeparately()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.SpikedTail, "Spiked Tail", 5));
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.SpikedTail, "Spiked Tail"));
        var service = new KeepCardRulesService();

        var bonusDamage = service.GetBonusAttackDamage(player, rolledAttackCount: 1);

        Assert.Equal(2, bonusDamage);
    }

    private static MarketCardState CreateMimicCopying(string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(1, copiedCardId, copiedCardName));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
    }
}
