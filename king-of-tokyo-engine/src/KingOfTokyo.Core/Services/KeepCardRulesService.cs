using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class KeepCardRulesService
{
    public const int BaseDiceCount = 6;
    public const int MinimumDiceCount = 1;
    public const int WingsCost = 2;
    public const string MetamorphCardId = KnownCardIds.Metamorph;
    public const string PlotTwistCardId = KnownCardIds.PlotTwist;
    public const string SmokeCloudCardId = KnownCardIds.SmokeCloud;

    public int GetEffectiveDiceCount(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var diceCount = BaseDiceCount + GetExtraDiceCount(player) - player.Status.ShrinkTokens;
        return Math.Max(MinimumDiceCount, diceCount);
    }

    public int GetExtraDiceCount(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var extraDice = 0;

        extraDice += CountKeepCardEffects(player, KnownCardIds.ExtraHead);

        return extraDice;
    }

    public int GetExtraRerolls(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var extraRerolls = 0;

        extraRerolls += CountKeepCardEffects(player, KnownCardIds.GiantBrain);

        return extraRerolls;
    }

    public int GetEffectivePurchaseCost(PlayerState player, MarketCardState card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        var discount = 0;

        if (player.HasKeepCard(KnownCardIds.AlienMetabolism))
        {
            discount += 1;
        }

        return Math.Max(0, card.Cost - discount);
    }

    public bool CanUseTelepath(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.HasKeepCard(KnownCardIds.Telepath) &&
               player.Energy >= 1;
    }

    public bool CanUseStretchy(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.HasKeepCard(KnownCardIds.Stretchy) &&
               player.Energy >= 2;
    }

    public bool CanUseHerdCuller(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.HasKeepCard(KnownCardIds.HerdCuller);
    }

    public bool CanUseMadeInALab(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.HasKeepCard(KnownCardIds.MadeInALab);
    }

    public bool CanUseWings(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.HasKeepCard(KnownCardIds.Wings) && player.Energy >= WingsCost;
    }

    public bool CanUseSmokeCloud(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.KeepCards.Any(card => card.CardId == KnownCardIds.SmokeCloud && card.Counters > 0);
    }

    public bool HasNovaBreath(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.HasKeepCard(KnownCardIds.NovaBreath);
    }

    public int GetIgnoredDamage(PlayerState player, DamageKind damageKind)
    {
        ArgumentNullException.ThrowIfNull(player);

        var ignoredDamage = 0;

        if (player.HasKeepCard(KnownCardIds.ArmorPlating))
        {
            ignoredDamage += 1;
        }

        return ignoredDamage;
    }

    public int GetBonusAttackDamage(PlayerState player, int rolledAttackCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (rolledAttackCount <= 0)
        {
            return 0;
        }

        var bonusDamage = 0;

        bonusDamage += CountKeepCardEffects(player, KnownCardIds.SpikedTail);
        bonusDamage += CountKeepCardEffects(player, KnownCardIds.AcidAttack);

        if (player.TokyoSlot != TokyoSlot.None &&
            HasKeepCardEffect(player, KnownCardIds.Urbavore))
        {
            bonusDamage += 1;
        }

        if (player.TokyoSlot == TokyoSlot.None &&
            HasKeepCardEffect(player, KnownCardIds.Burrowing))
        {
            bonusDamage += 1;
        }

        return bonusDamage;
    }

    public int GetAttackRewardVictoryPoints(PlayerState player, int rolledAttackCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (rolledAttackCount <= 0)
        {
            return 0;
        }

        if (player.HasKeepCard(KnownCardIds.AlphaMonster))
        {
            return 1;
        }

        return 0;
    }

    public int GetVictoryPointsWhenTakingDamage(PlayerState player, int actualDamage)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (actualDamage < 2 || !player.IsAlive)
        {
            return 0;
        }

        return player.HasKeepCard(KnownCardIds.WereOnlyMakingItStronger) ? 1 : 0;
    }

    public int GetHealingWhenLeavingTokyo(PlayerState player, int damageTaken)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (damageTaken <= 0 || !player.HasKeepCard(KnownCardIds.Jets))
        {
            return 0;
        }

        return damageTaken;
    }

    public int GetBonusScoringVictoryPoints(PlayerState player, int scoredVictoryPointsFromNumbers)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (scoredVictoryPointsFromNumbers <= 0)
        {
            return 0;
        }

        var bonusVictoryPoints = 0;

        if (player.HasKeepCard(KnownCardIds.Gourmet))
        {
            bonusVictoryPoints += 2;
        }

        return bonusVictoryPoints;
    }

    public int GetCompleteDestructionVictoryPoints(PlayerState player, int oneCount, int twoCount, int threeCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.HasKeepCard(KnownCardIds.CompleteDestruction))
        {
            return 0;
        }

        if (oneCount > 0 && twoCount > 0 && threeCount > 0)
        {
            return 9;
        }

        return 0;
    }

    public int GetPoisonQuillsDamage(PlayerState player, int oneCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.HasKeepCard(KnownCardIds.PoisonQuills))
        {
            return 0;
        }

        return oneCount >= 3 ? 2 : 0;
    }

    public int GetPoisonTokensToApply(PlayerState player, int attackCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (attackCount <= 0 || !player.HasKeepCard(KnownCardIds.PoisonSpit))
        {
            return 0;
        }

        return 1;
    }

    public int GetShrinkTokensToApply(PlayerState player, int attackCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (attackCount <= 0 || !player.HasKeepCard(KnownCardIds.ShrinkRay))
        {
            return 0;
        }

        return 1;
    }

    public bool HasBurrowing(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return HasKeepCardEffect(player, KnownCardIds.Burrowing);
    }

    public int GetBonusHealing(PlayerState player, int baseHealingAmount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (baseHealingAmount <= 0)
        {
            return 0;
        }

        var bonusHealing = 0;

        if (player.HasKeepCard(KnownCardIds.Regeneration))
        {
            bonusHealing += 1;
        }

        return bonusHealing;
    }

    public int GetEndTurnVictoryPoints(PlayerState player, bool dealtDamageThisTurn)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (dealtDamageThisTurn)
        {
            return 0;
        }

        var bonusVictoryPoints = 0;

        if (player.HasKeepCard(KnownCardIds.Herbivore))
        {
            bonusVictoryPoints += 1;
        }

        return bonusVictoryPoints;
    }

    public int GetBonusEnergyGain(PlayerState player, int baseEnergyGain)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (baseEnergyGain <= 0)
        {
            return 0;
        }

        var bonusEnergy = 0;

        if (player.HasKeepCard(KnownCardIds.FriendOfChildren))
        {
            bonusEnergy += 1;
        }

        return bonusEnergy;
    }

    public int GetBonusStartTurnTokyoVictoryPoints(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.TokyoSlot == TokyoSlot.None)
        {
            return 0;
        }

        var bonusVictoryPoints = 0;

        if (HasKeepCardEffect(player, KnownCardIds.Urbavore))
        {
            bonusVictoryPoints += 1;
        }

        return bonusVictoryPoints;
    }

    public bool CanUseRapidHealing(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.HasKeepCard(KnownCardIds.RapidHealing) &&
               player.Energy >= 2 &&
               player.Health < player.MaxHealth;
    }

    public int GetEndTurnEnergyGainWhenEmpty(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.HasKeepCard(KnownCardIds.SolarPowered))
        {
            return 0;
        }

        return player.Energy == 0 ? 1 : 0;
    }

    public int GetEndTurnVictoryPointsFromStoredEnergy(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.HasKeepCard(KnownCardIds.EnergyHoarder))
        {
            return 0;
        }

        return player.Energy / 6;
    }

    public int GetEndTurnUnderdogVictoryPoints(PlayerState player, bool hasFewestVictoryPoints)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.HasKeepCard(KnownCardIds.RootingForTheUnderdog))
        {
            return 0;
        }

        return hasFewestVictoryPoints ? 1 : 0;
    }

    public int GetCardPurchaseVictoryPoints(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.HasKeepCard(KnownCardIds.DedicatedNewsTeam))
        {
            return 1;
        }

        return 0;
    }

    public int GetVictoryPointsWhenMonsterEliminated(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsAlive)
        {
            return 0;
        }

        if (player.HasKeepCard(KnownCardIds.EaterOfTheDead))
        {
            return 3;
        }

        return 0;
    }

    private static bool HasKeepCardEffect(PlayerState player, string cardId)
    {
        return CountKeepCardEffects(player, cardId) > 0;
    }

    private static int CountKeepCardEffects(PlayerState player, string cardId)
    {
        return player.KeepCards.Count(card =>
            card.CardId == cardId ||
            (card.CardId == KnownCardIds.Mimic && card.MimicTarget?.CardId == cardId));
    }
}
