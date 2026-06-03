using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.CardMimic;

public sealed class MimicKeepCardRulesServiceTests
{
    [Fact]
    public void GetEffectivePurchaseCost_Should_CountMimicCopyingAlienMetabolism()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.AlienMetabolism, "Alien Metabolism"));
        var card = CreateKeepCard("card-test", "Test Card", 3);
        var service = new KeepCardRulesService();

        var cost = service.GetEffectivePurchaseCost(player, card);

        Assert.Equal(2, cost);
    }

    [Fact]
    public void GetAttackRewardVictoryPoints_Should_CountMimicCopyingAlphaMonster()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.AlphaMonster, "Alpha Monster"));
        var service = new KeepCardRulesService();

        var victoryPoints = service.GetAttackRewardVictoryPoints(player, rolledAttackCount: 1);

        Assert.Equal(1, victoryPoints);
    }

    [Fact]
    public void GetBonusScoringVictoryPoints_Should_CountMimicCopyingGourmet()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Gourmet, "Gourmet"));
        var service = new KeepCardRulesService();

        var victoryPoints = service.GetBonusScoringVictoryPoints(player, scoredVictoryPointsFromNumbers: 3);

        Assert.Equal(2, victoryPoints);
    }

    [Fact]
    public void GetBonusHealing_Should_CountMimicCopyingRegeneration()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Regeneration, "Regeneration"));
        var service = new KeepCardRulesService();

        var bonusHealing = service.GetBonusHealing(player, baseHealingAmount: 1);

        Assert.Equal(1, bonusHealing);
    }

    [Fact]
    public void GetBonusEnergyGain_Should_CountMimicCopyingFriendOfChildren()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.FriendOfChildren, "Friend of Children"));
        var service = new KeepCardRulesService();

        var bonusEnergy = service.GetBonusEnergyGain(player, baseEnergyGain: 1);

        Assert.Equal(1, bonusEnergy);
    }

    [Fact]
    public void GetCardPurchaseVictoryPoints_Should_CountMimicCopyingDedicatedNewsTeam()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.DedicatedNewsTeam, "Dedicated News Team"));
        var service = new KeepCardRulesService();

        var victoryPoints = service.GetCardPurchaseVictoryPoints(player);

        Assert.Equal(1, victoryPoints);
    }

    [Fact]
    public void GetPoisonAndShrinkTokensToApply_Should_CountMimicCopiedTokenCards()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.PoisonSpit, "Poison Spit"));
        player.AddKeepCard(CreateKeepCard(KnownCardIds.ShrinkRay, "Shrink Ray", 6));
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.ShrinkRay, "Shrink Ray"));
        var service = new KeepCardRulesService();

        var poisonTokens = service.GetPoisonTokensToApply(player, attackCount: 1);
        var shrinkTokens = service.GetShrinkTokensToApply(player, attackCount: 1);

        Assert.Equal(1, poisonTokens);
        Assert.Equal(2, shrinkTokens);
    }

    [Theory]
    [InlineData(KnownCardIds.Herbivore, 1)]
    [InlineData(KnownCardIds.SolarPowered, 1)]
    [InlineData(KnownCardIds.RootingForTheUnderdog, 1)]
    public void EndTurnRules_Should_CountMimicCopiedEndTurnCards(string copiedCardId, int expectedValue)
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(copiedCardId, copiedCardId));
        var service = new KeepCardRulesService();

        var actualValue = copiedCardId switch
        {
            KnownCardIds.Herbivore => service.GetEndTurnVictoryPoints(player, dealtDamageThisTurn: false),
            KnownCardIds.SolarPowered => service.GetEndTurnEnergyGainWhenEmpty(player),
            KnownCardIds.RootingForTheUnderdog => service.GetEndTurnUnderdogVictoryPoints(player, hasFewestVictoryPoints: true),
            _ => throw new InvalidOperationException("Unsupported test card id.")
        };

        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void GetVictoryPointsWhenMonsterEliminated_Should_CountMimicCopyingEaterOfTheDead()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.EaterOfTheDead, "Eater of the Dead"));
        var service = new KeepCardRulesService();

        var victoryPoints = service.GetVictoryPointsWhenMonsterEliminated(player);

        Assert.Equal(3, victoryPoints);
    }

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
