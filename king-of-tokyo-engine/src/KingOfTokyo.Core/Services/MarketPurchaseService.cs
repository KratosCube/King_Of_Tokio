using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Rules.Tokyo;
using KingOfTokyo.Core.Rules.Victory;

namespace KingOfTokyo.Core.Services;

public sealed class MarketPurchaseService
{
    private readonly KeepCardRulesService _keepCardRulesService;
    private readonly DamageApplier _damageApplier;
    private readonly EliminationService _eliminationService;
    private readonly TokyoResolver _tokyoResolver;
    private readonly EnergyPaymentService _energyPaymentService;

    public MarketPurchaseService(
        KeepCardRulesService? keepCardRulesService = null,
        DamageApplier? damageApplier = null,
        EliminationService? eliminationService = null,
        TokyoResolver? tokyoResolver = null,
        EnergyPaymentService? energyPaymentService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _damageApplier = damageApplier ?? new DamageApplier();
        _eliminationService = eliminationService ?? new EliminationService();
        _tokyoResolver = tokyoResolver ?? new TokyoResolver();
        _energyPaymentService = energyPaymentService ?? new EnergyPaymentService();
    }

    public EngineStepResult BuyFaceUpCard(GameState gameState, int slotIndex, int effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot buy a card without an active turn.");

        var player = gameState.GetCurrentPlayer();
        var card = gameState.Market.FaceUpCards[slotIndex];

        if (card is null)
        {
            throw new InvalidOperationException("Selected market slot is empty.");
        }

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var boughtCard = gameState.Market.RemoveFaceUpCardAt(slotIndex);

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        var pendingDecision = CreateOpportunistDecisionForSlot(gameState, slotIndex);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(events, pendingDecision);
    }

    public EngineStepResult BuyOpportunistRevealedCard(GameState gameState, int actorPlayerId, int slotIndex, int effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot buy a card without an active turn.");

        var player = gameState.GetPlayerById(actorPlayerId);
        var card = gameState.Market.FaceUpCards[slotIndex];

        if (card is null)
        {
            throw new InvalidOperationException("Selected market slot is empty.");
        }

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var boughtCard = gameState.Market.RemoveFaceUpCardAt(slotIndex);

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        var pendingDecision = CreateOpportunistDecisionForSlot(gameState, slotIndex);
        gameState.SetPendingDecision(pendingDecision);

        return new EngineStepResult(events, pendingDecision);
    }

    public EngineStepResult BuyTopDeckCard(GameState gameState, int effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot buy a card without an active turn.");

        var player = gameState.GetCurrentPlayer();
        var boughtCard = gameState.Market.RemoveTopDrawCard();

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        return new EngineStepResult(events);
    }

    private List<GameEventBase> FinalizePurchasedCard(
        GameState gameState,
        TurnState currentTurn,
        PlayerState player,
        MarketCardState boughtCard,
        int effectiveCost)
    {
        var events = new List<GameEventBase>
        {
            new CardBoughtEvent(
                player.PlayerId,
                boughtCard.CardId,
                boughtCard.Name,
                effectiveCost,
                boughtCard.CardType)
        };

        ApplyPurchaseEffect(gameState, player, boughtCard, currentTurn, events);

        var dedicatedNewsTeamPoints = _keepCardRulesService.GetCardPurchaseVictoryPoints(player);
        if (dedicatedNewsTeamPoints > 0)
        {
            player.GainVictoryPoints(dedicatedNewsTeamPoints);
            events.Add(new VictoryPointsGainedEvent(
                player.PlayerId,
                dedicatedNewsTeamPoints,
                "Keep card: Dedicated News Team."));
        }

        if (boughtCard.CardType == MarketCardType.Keep)
        {
            player.AddKeepCard(boughtCard);
        }
        else
        {
            gameState.Market.Discard(boughtCard);
        }

        return events;
    }

    private void ApplyPurchaseEffect(
        GameState gameState,
        PlayerState player,
        MarketCardState boughtCard,
        TurnState currentTurn,
        List<GameEventBase> events)
    {
        var effect = boughtCard.PurchaseEffect;

        if (effect.IncreaseMaxHealth > 0)
        {
            player.IncreaseMaxHealth(effect.IncreaseMaxHealth);
        }

        if (effect.GainVictoryPoints > 0)
        {
            player.GainVictoryPoints(effect.GainVictoryPoints);
            events.Add(new VictoryPointsGainedEvent(
                player.PlayerId,
                effect.GainVictoryPoints,
                $"Bought card: {boughtCard.Name}."));
        }

        if (effect.GainEnergy > 0)
        {
            var bonusEnergy = _keepCardRulesService.GetBonusEnergyGain(player, effect.GainEnergy);
            var totalEnergy = effect.GainEnergy + bonusEnergy;

            player.GainEnergy(totalEnergy);
            events.Add(new EnergyGainedEvent(
                player.PlayerId,
                totalEnergy,
                bonusEnergy > 0
                    ? $"Bought card: {boughtCard.Name} + Friend of Children."
                    : $"Bought card: {boughtCard.Name}."));
        }

        if (effect.Heal > 0)
        {
            var bonusHealing = _keepCardRulesService.GetBonusHealing(player, effect.Heal);
            var totalHealing = effect.Heal + bonusHealing;

            var healthBefore = player.Health;
            player.Heal(totalHealing);
            var actualHealed = player.Health - healthBefore;

            if (actualHealed > 0)
            {
                events.Add(new PlayerHealedEvent(
                    player.PlayerId,
                    actualHealed,
                    bonusHealing > 0
                        ? $"Bought card: {boughtCard.Name} + Regeneration."
                        : $"Bought card: {boughtCard.Name}."));
            }
        }

        if (effect.EnterTokyo && player.TokyoSlot == TokyoSlot.None && _tokyoResolver.GetPreferredAvailableSlot(gameState) is not null)
        {
            var enteredSlot = _tokyoResolver.EnterTokyo(gameState, player);
            currentTurn.Flags.EnteredTokyo = true;

            events.Add(new TokyoEnteredEvent(player.PlayerId, enteredSlot));
        }

        if (boughtCard.CardId == KnownCardIds.Frenzy)
        {
            gameState.ScheduleExtraTurn(player.PlayerId);
        }

        if (effect.DamageAllOthers > 0)
        {
            ApplyCardEffectDamageToTargets(
                gameState,
                currentTurn,
                player,
                gameState.Players.Where(p => p.PlayerId != player.PlayerId && p.IsAlive),
                effect.DamageAllOthers,
                boughtCard.Name,
                events);
        }

        if (effect.DamageAllIncludingSelf > 0)
        {
            ApplyCardEffectDamageToTargets(
                gameState,
                currentTurn,
                player,
                gameState.Players.Where(p => p.IsAlive),
                effect.DamageAllIncludingSelf,
                boughtCard.Name,
                events);
        }

        if (effect.DamageSelf > 0)
        {
            ApplyCardEffectDamageToTargets(
                gameState,
                currentTurn,
                player,
                new[] { player },
                effect.DamageSelf,
                boughtCard.Name,
                events);
        }

        if (effect.DamageOthersPerTwoEnergy > 0)
        {
            foreach (var target in gameState.Players.Where(p => p.PlayerId != player.PlayerId && p.IsAlive).ToArray())
            {
                var damage = (target.Energy / 2) * effect.DamageOthersPerTwoEnergy;
                if (damage <= 0)
                {
                    continue;
                }

                ApplyCardEffectDamageToTargets(
                    gameState,
                    currentTurn,
                    player,
                    new[] { target },
                    damage,
                    boughtCard.Name,
                    events);
            }
        }
    }

    private void ApplyCardEffectDamageToTargets(
        GameState gameState,
        TurnState currentTurn,
        PlayerState sourcePlayer,
        IEnumerable<PlayerState> targets,
        int amount,
        string sourceName,
        List<GameEventBase> events)
    {
        foreach (var target in targets.ToArray())
        {
            var packet = new DamagePacket
            {
                SourcePlayerId = sourcePlayer.PlayerId,
                TargetPlayerId = target.PlayerId,
                Amount = amount,
                DamageKind = DamageKind.CardEffect,
                CountsAsAttack = false,
                AllowsTokyoLeave = false
            };

            var actualDamage = _damageApplier.ApplyDamage(target, packet);
            if (actualDamage <= 0)
            {
                continue;
            }

            if (target.PlayerId != sourcePlayer.PlayerId)
            {
                currentTurn.Flags.DealtDamage = true;
            }

            events.Add(new DamageDealtEvent(
                sourcePlayer.PlayerId,
                target.PlayerId,
                actualDamage,
                DamageKind.CardEffect));

            if (!target.IsAlive && _eliminationService.TryEliminate(gameState, target))
            {
                currentTurn.Flags.EliminatedSomeone = true;

                events.Add(new PlayerEliminatedEvent(
                    target.PlayerId,
                    sourcePlayer.PlayerId,
                    $"Bought card: {sourceName}."));

                AwardEaterOfTheDeadPoints(gameState, events);
            }
        }
    }

    private PendingDecision? CreateOpportunistDecisionForSlot(GameState gameState, int slotIndex)
    {
        var revealedCard = gameState.Market.FaceUpCards[slotIndex];
        if (revealedCard is null)
        {
            return null;
        }

        var eligiblePlayerIds = gameState.Players
            .Where(player => player.IsAlive &&
                             player.HasKeepCard(KnownCardIds.Opportunist) &&
                             _energyPaymentService.GetAvailableEnergy(player) >= _keepCardRulesService.GetEffectivePurchaseCost(player, revealedCard))
            .Select(player => player.PlayerId)
            .ToArray();

        if (eligiblePlayerIds.Length == 0)
        {
            return null;
        }

        return new PendingDecision
        {
            DecisionType = DecisionType.OpportunistPurchase,
            PlayerId = eligiblePlayerIds[0],
            Payload = new MarketCardRevealDecisionData
            {
                SlotIndex = slotIndex,
                CardId = revealedCard.CardId,
                CardName = revealedCard.Name,
                Cost = _keepCardRulesService.GetEffectivePurchaseCost(gameState.GetPlayerById(eligiblePlayerIds[0]), revealedCard),
                EligiblePlayerIds = eligiblePlayerIds
            }
        };
    }

    private void AwardEaterOfTheDeadPoints(GameState gameState, List<GameEventBase> events)
    {
        foreach (var alivePlayer in gameState.GetAlivePlayers())
        {
            var bonusVictoryPoints = _keepCardRulesService.GetVictoryPointsWhenMonsterEliminated(alivePlayer);
            if (bonusVictoryPoints <= 0)
            {
                continue;
            }

            alivePlayer.GainVictoryPoints(bonusVictoryPoints);

            events.Add(new VictoryPointsGainedEvent(
                alivePlayer.PlayerId,
                bonusVictoryPoints,
                "Keep card: Eater of the Dead."));
        }
    }
}
