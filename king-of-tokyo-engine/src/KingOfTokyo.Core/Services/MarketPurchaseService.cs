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
    private readonly KeepCardLifecycleService _keepCardLifecycleService;
    private readonly DamageApplier _damageApplier;
    private readonly EliminationService _eliminationService;
    private readonly TokyoResolver _tokyoResolver;
    private readonly EnergyPaymentService _energyPaymentService;

    public MarketPurchaseService(
        KeepCardRulesService? keepCardRulesService = null,
        KeepCardLifecycleService? keepCardLifecycleService = null,
        DamageApplier? damageApplier = null,
        EliminationService? eliminationService = null,
        TokyoResolver? tokyoResolver = null,
        EnergyPaymentService? energyPaymentService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _keepCardLifecycleService = keepCardLifecycleService ?? new KeepCardLifecycleService();
        _damageApplier = damageApplier ?? new DamageApplier();
        _eliminationService = eliminationService ?? new EliminationService();
        _tokyoResolver = tokyoResolver ?? new TokyoResolver();
        _energyPaymentService = energyPaymentService ?? new EnergyPaymentService();
    }

    public EngineStepResult BuyFaceUpCard(GameState gameState, int slotIndex, int effectiveCost, int storedEnergyToDeposit = 0)
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

        ValidateDeposit(player, card, effectiveCost, storedEnergyToDeposit);

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var boughtCard = gameState.Market.RemoveFaceUpCardAt(slotIndex);

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost, storedEnergyToDeposit);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        var pendingDecision = gameState.QueueOpportunistDecisions(CreateOpportunistDecisionsForSlot(gameState, slotIndex));

        return new EngineStepResult(events, pendingDecision);
    }

    public EngineStepResult BuyOpportunistRevealedCard(GameState gameState, int actorPlayerId, int slotIndex, int effectiveCost, int storedEnergyToDeposit = 0)
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

        ValidateDeposit(player, card, effectiveCost, storedEnergyToDeposit);

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var boughtCard = gameState.Market.RemoveFaceUpCardAt(slotIndex);

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost, storedEnergyToDeposit);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        var pendingDecision = gameState.QueueOpportunistDecisions(CreateOpportunistDecisionsForSlot(gameState, slotIndex));

        return new EngineStepResult(events, pendingDecision);
    }

    public EngineStepResult BuyTopDeckCard(GameState gameState, int effectiveCost, int storedEnergyToDeposit = 0)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot buy a card without an active turn.");

        var player = gameState.GetCurrentPlayer();
        ValidateDeposit(player, gameState.Market.PeekTopDrawCard(), effectiveCost, storedEnergyToDeposit);
        var boughtCard = gameState.Market.RemoveTopDrawCard();

        var paymentEvents = _energyPaymentService.SpendEnergy(
            gameState,
            player,
            effectiveCost,
            "Keep card: Monster Batteries.");

        var events = FinalizePurchasedCard(gameState, currentTurn, player, boughtCard, effectiveCost, storedEnergyToDeposit);
        events.InsertRange(0, paymentEvents);

        currentTurn.Flags.BoughtCard = true;

        return new EngineStepResult(events);
    }

    private static void ValidateDeposit(PlayerState player, MarketCardState card, int effectiveCost, int amount)
    {
        if (amount < 0 || amount > player.Energy - effectiveCost ||
            (card.CardId != KnownCardIds.MonsterBatteries && amount != 0))
        {
            throw new InvalidOperationException("Invalid energy deposit for Monster Batteries.");
        }
    }

    private List<GameEventBase> FinalizePurchasedCard(
        GameState gameState,
        TurnState currentTurn,
        PlayerState player,
        MarketCardState boughtCard,
        int effectiveCost,
        int storedEnergyToDeposit)
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

        if (boughtCard.CardId == KnownCardIds.MonsterBatteries && storedEnergyToDeposit > 0)
        {
            player.SpendEnergy(storedEnergyToDeposit);
            boughtCard.AddStoredEnergy(2 * storedEnergyToDeposit);
        }

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

        if (boughtCard.CardId == KnownCardIds.MonsterBatteries && boughtCard.StoredEnergy == 0)
        {
            gameState.Market.Discard(boughtCard);
            events.Add(new KeepCardDiscardedEvent(player.PlayerId, boughtCard.CardId, boughtCard.Name, "Empty Monster Batteries."));
        }
        else if (boughtCard.CardType == MarketCardType.Keep)
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

        if (boughtCard.CardType == MarketCardType.Keep)
        {
            _keepCardLifecycleService.ApplyAddedEffect(player, boughtCard);
        }
        else if (effect.IncreaseMaxHealth > 0)
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

        if (boughtCard.CardId == KnownCardIds.DropFromHighAltitude && player.TokyoSlot == TokyoSlot.None)
        {
            if (gameState.Tokyo.CityOccupantId is int occupantId)
            {
                var occupant = gameState.GetPlayerById(occupantId);
                _tokyoResolver.LeaveTokyo(gameState, occupant);
                events.Add(new TokyoLeftEvent(occupantId, TokyoSlot.City));
            }

            var enteredSlot = _tokyoResolver.EnterTokyo(gameState, player);
            currentTurn.Flags.EnteredTokyo = true;
            events.Add(new TokyoEnteredEvent(player.PlayerId, enteredSlot));
        }
        else if (effect.EnterTokyo && player.TokyoSlot == TokyoSlot.None && _tokyoResolver.GetPreferredAvailableSlot(gameState) is not null)
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
            var targetDamage = amount + (target.PlayerId == sourcePlayer.PlayerId
                ? 0
                : _keepCardRulesService.GetAcidAttackBonusDamage(sourcePlayer, amount));

            var packet = new DamagePacket
            {
                SourcePlayerId = sourcePlayer.PlayerId,
                TargetPlayerId = target.PlayerId,
                Amount = targetDamage,
                DamageKind = DamageKind.CardEffect,
                CountsAsAttack = false,
                AllowsTokyoLeave = false
            };

            if (currentTurn.IsProtectedByWings(target.PlayerId))
            {
                events.Add(new DamagePreventedEvent(packet.SourcePlayerId, target.PlayerId,
                    packet.Amount, packet.DamageKind, "Keep card: Wings."));
                continue;
            }

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

    private IReadOnlyList<PendingDecision> CreateOpportunistDecisionsForSlot(GameState gameState, int slotIndex)
    {
        var revealedCard = gameState.Market.FaceUpCards[slotIndex];
        if (revealedCard is null)
        {
            return Array.Empty<PendingDecision>();
        }

        var eligiblePlayerIds = gameState.Players
            .Where(player => player.IsAlive &&
                             player.HasKeepCard(KnownCardIds.Opportunist) &&
                             _energyPaymentService.GetAvailableEnergy(player) >= _keepCardRulesService.GetEffectivePurchaseCost(player, revealedCard))
            .Select(player => player.PlayerId)
            .ToArray();

        if (eligiblePlayerIds.Length == 0)
        {
            return Array.Empty<PendingDecision>();
        }

        return eligiblePlayerIds.Select(playerId => new PendingDecision
        {
            DecisionType = DecisionType.OpportunistPurchase,
            PlayerId = playerId,
            Payload = new MarketCardRevealDecisionData
            {
                SlotIndex = slotIndex,
                CardId = revealedCard.CardId,
                CardName = revealedCard.Name,
                Cost = _keepCardRulesService.GetEffectivePurchaseCost(gameState.GetPlayerById(playerId), revealedCard),
                EligiblePlayerIds = eligiblePlayerIds
            }
        }).ToArray();
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
