using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class KeepCardRulesService
{
    public const int BaseDiceCount = 6;
    public const int MinimumDiceCount = 1;

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

        if (player.HasKeepCard(KnownCardIds.ExtraHead))
        {
            extraDice += 1;
        }

        return extraDice;
    }

    public int GetExtraRerolls(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var extraRerolls = 0;

        if (player.HasKeepCard(KnownCardIds.GiantBrain))
        {
            extraRerolls += 1;
        }

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

    public bool CanUseMadeInALab(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.HasKeepCard(KnownCardIds.MadeInALab);
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

        if (player.HasKeepCard(KnownCardIds.SpikedTail))
        {
            bonusDamage += 1;
        }

        if (player.TokyoSlot != TokyoSlot.None &&
            player.HasKeepCard(KnownCardIds.Urbavore))
        {
            bonusDamage += 1;
        }

        if (player.TokyoSlot == TokyoSlot.None &&
            player.HasKeepCard(KnownCardIds.Burrowing))
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
        return player.HasKeepCard(KnownCardIds.Burrowing);
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

        if (player.HasKeepCard(KnownCardIds.Urbavore))
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
}
