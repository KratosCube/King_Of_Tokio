using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Rules.Healing;
using KingOfTokyo.Core.Rules.Scoring;
using KingOfTokyo.Core.Rules.Tokyo;
using KingOfTokyo.Core.Rules.Victory;

namespace KingOfTokyo.Core.Services;

public sealed class FinalizeDiceService
{
    private readonly DiceSummaryBuilder _diceSummaryBuilder;
    private readonly ScoringResolver _scoringResolver;
    private readonly EnergyResolver _energyResolver;
    private readonly HealingResolver _healingResolver;
    private readonly AttackResolver _attackResolver;
    private readonly DamageApplier _damageApplier;
    private readonly TokyoResolver _tokyoResolver;
    private readonly EliminationService _eliminationService;
    private readonly KeepCardRulesService _keepCardRulesService;

    public FinalizeDiceService(
        DiceSummaryBuilder? diceSummaryBuilder = null,
        ScoringResolver? scoringResolver = null,
        EnergyResolver? energyResolver = null,
        HealingResolver? healingResolver = null,
        AttackResolver? attackResolver = null,
        DamageApplier? damageApplier = null,
        TokyoResolver? tokyoResolver = null,
        EliminationService? eliminationService = null,
        KeepCardRulesService? keepCardRulesService = null)
    {
        _diceSummaryBuilder = diceSummaryBuilder ?? new DiceSummaryBuilder();
        _scoringResolver = scoringResolver ?? new ScoringResolver();
        _energyResolver = energyResolver ?? new EnergyResolver();
        _healingResolver = healingResolver ?? new HealingResolver();
        _attackResolver = attackResolver ?? new AttackResolver();
        _damageApplier = damageApplier ?? new DamageApplier();
        _tokyoResolver = tokyoResolver ?? new TokyoResolver();
        _eliminationService = eliminationService ?? new EliminationService();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public EngineStepResult Execute(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot finalize dice without an active turn.");

        var currentPlayer = gameState.GetCurrentPlayer();

        gameState.ClearPendingDecision();

        var summary = _diceSummaryBuilder.Build(currentTurn.DicePool);
        currentTurn.MarkDiceResolved();

        var newEvents = new List<GameEventBase>
        {
            new DiceFinalizedEvent(currentPlayer.PlayerId, summary)
        };

        var victoryPoints = _scoringResolver.ResolveVictoryPoints(summary);
        if (victoryPoints > 0)
        {
            currentPlayer.GainVictoryPoints(victoryPoints);
            currentTurn.Flags.ScoredVictoryPoints = true;

            newEvents.Add(new VictoryPointsGainedEvent(
                currentPlayer.PlayerId,
                victoryPoints,
                "Dice scoring."));
        }

        ScheduleFreezeTimeExtraTurnIfEligible(gameState, currentPlayer, summary, victoryPoints);

        var gourmetBonus = _keepCardRulesService.GetBonusScoringVictoryPoints(currentPlayer, victoryPoints);
        if (gourmetBonus > 0)
        {
            currentPlayer.GainVictoryPoints(gourmetBonus);
            currentTurn.Flags.ScoredVictoryPoints = true;

            newEvents.Add(new VictoryPointsGainedEvent(
                currentPlayer.PlayerId,
                gourmetBonus,
                "Keep card: Gourmet."));
        }

        var completeDestructionBonus = _keepCardRulesService.GetCompleteDestructionVictoryPoints(
            currentPlayer,
            summary.OneCount,
            summary.TwoCount,
            summary.ThreeCount);

        if (completeDestructionBonus > 0)
        {
            currentPlayer.GainVictoryPoints(completeDestructionBonus);
            currentTurn.Flags.ScoredVictoryPoints = true;

            newEvents.Add(new VictoryPointsGainedEvent(
                currentPlayer.PlayerId,
                completeDestructionBonus,
                "Keep card: Complete Destruction."));
        }

        var omnivoreBonus = _keepCardRulesService.GetOmnivoreVictoryPoints(
            currentPlayer,
            summary.AttackCount,
            summary.EnergyCount,
            summary.HeartCount,
            summary.OneCount,
            summary.TwoCount,
            summary.ThreeCount);

        if (omnivoreBonus > 0)
        {
            currentPlayer.GainVictoryPoints(omnivoreBonus);
            currentTurn.Flags.ScoredVictoryPoints = true;

            newEvents.Add(new VictoryPointsGainedEvent(
                currentPlayer.PlayerId,
                omnivoreBonus,
                "Keep card: Omnivore."));
        }

        var baseEnergy = _energyResolver.ResolveEnergy(summary);
        var bonusEnergy = _keepCardRulesService.GetBonusEnergyGain(currentPlayer, baseEnergy);
        var totalEnergy = baseEnergy + bonusEnergy;

        if (totalEnergy > 0)
        {
            currentPlayer.GainEnergy(totalEnergy);

            newEvents.Add(new EnergyGainedEvent(
                currentPlayer.PlayerId,
                totalEnergy,
                bonusEnergy > 0 ? "Dice energy + Friend of Children." : "Dice energy."));
        }

        var heartsRemainingForHealing = RemoveStatusTokensWithHearts(currentPlayer, summary.HeartCount, newEvents);
        var healingSummary = summary with { HeartCount = heartsRemainingForHealing };
        var healedAmount = _healingResolver.ResolveHealing(currentPlayer, healingSummary);
        var regenerationBonus = _keepCardRulesService.GetBonusHealing(currentPlayer, healedAmount);
        var totalHealing = healedAmount + regenerationBonus;

        if (totalHealing > 0)
        {
            var healthBefore = currentPlayer.Health;
            currentPlayer.Heal(totalHealing);
            var actualHealing = currentPlayer.Health - healthBefore;

            if (actualHealing > 0)
            {
                newEvents.Add(new PlayerHealedEvent(
                    currentPlayer.PlayerId,
                    actualHealing,
                    regenerationBonus > 0 ? "Dice healing + Regeneration." : "Dice healing."));
            }
        }

        ApplyPoisonQuillsDamage(gameState, currentPlayer, currentTurn, newEvents, summary);

        if (summary.AttackCount > 0)
        {
            currentTurn.Flags.AttackedWithDice = true;

            var alphaMonsterPoints = _keepCardRulesService.GetAttackRewardVictoryPoints(currentPlayer, summary.AttackCount);
            if (alphaMonsterPoints > 0)
            {
                currentPlayer.GainVictoryPoints(alphaMonsterPoints);
                currentTurn.Flags.ScoredVictoryPoints = true;

                newEvents.Add(new VictoryPointsGainedEvent(
                    currentPlayer.PlayerId,
                    alphaMonsterPoints,
                    "Keep card: Alpha Monster."));
            }
        }

        if (summary.AttackCount > 0 &&
            currentPlayer.TokyoSlot == TokyoSlot.None &&
            _tokyoResolver.IsTokyoCompletelyEmpty(gameState))
        {
            var enteredSlot = _tokyoResolver.EnterTokyo(gameState, currentPlayer);
            currentPlayer.GainVictoryPoints(1);
            currentTurn.Flags.EnteredTokyo = true;
            currentTurn.Flags.ScoredVictoryPoints = true;
            currentTurn.SetPhase(TurnPhase.Purchase);

            newEvents.Add(new TokyoEnteredEvent(currentPlayer.PlayerId, enteredSlot));
            newEvents.Add(new VictoryPointsGainedEvent(
                currentPlayer.PlayerId,
                1,
                "Entered Tokyo."));

            return new EngineStepResult(newEvents);
        }

        var damagePackets = _attackResolver.ResolveAttack(gameState, currentPlayer, summary);
        var tokyoLeaveContexts = new List<TokyoLeaveDecisionContext>();

        foreach (var packet in damagePackets)
        {
            var target = gameState.GetPlayerById(packet.TargetPlayerId);
            var wasInTokyoBeforeDamage = target.TokyoSlot != TokyoSlot.None;

            ApplyAttackStatusTokens(currentPlayer, target, summary.AttackCount, newEvents);

            var actualDamage = _damageApplier.ApplyDamage(target, packet);

            if (actualDamage <= 0)
            {
                continue;
            }

            currentTurn.Flags.DealtDamage = true;

            newEvents.Add(new DamageDealtEvent(
                packet.SourcePlayerId,
                packet.TargetPlayerId,
                actualDamage,
                packet.DamageKind));

            if (!target.IsAlive)
            {
                if (_eliminationService.TryEliminate(gameState, target))
                {
                    currentTurn.Flags.EliminatedSomeone = true;

                    newEvents.Add(new PlayerEliminatedEvent(
                        target.PlayerId,
                        currentPlayer.PlayerId,
                        "Attack damage."));

                    AwardEaterOfTheDeadPoints(gameState, newEvents);

                    if (packet.AllowsTokyoLeave &&
                        wasInTokyoBeforeDamage &&
                        currentPlayer.TokyoSlot == TokyoSlot.None &&
                        _tokyoResolver.GetPreferredAvailableSlot(gameState) is not null)
                    {
                        var enteredSlot = _tokyoResolver.EnterTokyo(gameState, currentPlayer);
                        currentPlayer.GainVictoryPoints(1);
                        currentTurn.Flags.EnteredTokyo = true;
                        currentTurn.Flags.ScoredVictoryPoints = true;

                        newEvents.Add(new TokyoEnteredEvent(currentPlayer.PlayerId, enteredSlot));
                        newEvents.Add(new VictoryPointsGainedEvent(
                            currentPlayer.PlayerId,
                            1,
                            "Entered Tokyo after elimination."));
                    }
                }

                continue;
            }

            if (packet.AllowsTokyoLeave && target.TokyoSlot != TokyoSlot.None)
            {
                tokyoLeaveContexts.Add(new TokyoLeaveDecisionContext
                {
                    AttackerPlayerId = packet.SourcePlayerId,
                    DefenderPlayerId = packet.TargetPlayerId,
                    DamageTaken = actualDamage
                });
            }
        }

        if (tokyoLeaveContexts.Count > 0)
        {
            currentTurn.ClearTokyoLeaveDecisions();
            currentTurn.EnqueueTokyoLeaveDecisions(tokyoLeaveContexts);

            var firstDecision = tokyoLeaveContexts[0];
            var pendingDecision = new PendingDecision
            {
                DecisionType = DecisionType.LeaveTokyo,
                PlayerId = firstDecision.DefenderPlayerId,
                Payload = new LeaveTokyoDecisionData
                {
                    AttackerPlayerId = firstDecision.AttackerPlayerId,
                    DamageTaken = firstDecision.DamageTaken,
                    RemainingDefenderPlayerIds = tokyoLeaveContexts
                        .Select(context => context.DefenderPlayerId)
                        .ToArray()
                }
            };

            gameState.SetPendingDecision(pendingDecision);
            currentTurn.SetPhase(TurnPhase.WaitingForTokyoDecision);
            return new EngineStepResult(newEvents, pendingDecision);
        }

        currentTurn.SetPhase(TurnPhase.Purchase);
        return new EngineStepResult(newEvents);
    }

    private void ApplyPoisonQuillsDamage(
        GameState gameState,
        PlayerState currentPlayer,
        TurnState currentTurn,
        List<GameEventBase> newEvents,
        DiceResolutionSummary summary)
    {
        var poisonQuillsDamage = _keepCardRulesService.GetPoisonQuillsDamage(currentPlayer, summary.OneCount);
        if (poisonQuillsDamage <= 0)
        {
            return;
        }

        var targets = currentPlayer.TokyoSlot == TokyoSlot.None
            ? gameState.GetAlivePlayers().Where(player => player.TokyoSlot != TokyoSlot.None)
            : gameState.GetAlivePlayers().Where(player => player.PlayerId != currentPlayer.PlayerId && player.TokyoSlot == TokyoSlot.None);

        foreach (var target in targets.ToArray())
        {
            var actualDamage = target.TakeDamage(poisonQuillsDamage);
            if (actualDamage <= 0)
            {
                continue;
            }

            currentTurn.Flags.DealtDamage = true;

            newEvents.Add(new DamageDealtEvent(
                currentPlayer.PlayerId,
                target.PlayerId,
                actualDamage,
                DamageKind.CardEffect));

            if (!target.IsAlive && _eliminationService.TryEliminate(gameState, target))
            {
                currentTurn.Flags.EliminatedSomeone = true;

                newEvents.Add(new PlayerEliminatedEvent(
                    target.PlayerId,
                    currentPlayer.PlayerId,
                    "Poison Quills."));

                AwardEaterOfTheDeadPoints(gameState, newEvents);
            }
        }
    }

    private void ApplyAttackStatusTokens(
        PlayerState attacker,
        PlayerState target,
        int attackCount,
        List<GameEventBase> newEvents)
    {
        var poisonTokens = _keepCardRulesService.GetPoisonTokensToApply(attacker, attackCount);
        var shrinkTokens = _keepCardRulesService.GetShrinkTokensToApply(attacker, attackCount);

        if (poisonTokens <= 0 && shrinkTokens <= 0)
        {
            return;
        }

        target.Status.AddPoisonTokens(poisonTokens);
        target.Status.AddShrinkTokens(shrinkTokens);

        newEvents.Add(new StatusTokensAddedEvent(
            attacker.PlayerId,
            target.PlayerId,
            poisonTokens,
            shrinkTokens));
    }

    private void AwardEaterOfTheDeadPoints(GameState gameState, List<GameEventBase> newEvents)
    {
        foreach (var player in gameState.GetAlivePlayers())
        {
            var bonusVictoryPoints = _keepCardRulesService.GetVictoryPointsWhenMonsterEliminated(player);
            if (bonusVictoryPoints <= 0)
            {
                continue;
            }

            player.GainVictoryPoints(bonusVictoryPoints);

            newEvents.Add(new VictoryPointsGainedEvent(
                player.PlayerId,
                bonusVictoryPoints,
                "Keep card: Eater of the Dead."));
        }
    }

    private static void ScheduleFreezeTimeExtraTurnIfEligible(
        GameState gameState,
        PlayerState currentPlayer,
        DiceResolutionSummary summary,
        int scoredVictoryPoints)
    {
        if (summary.OneCount < 3 ||
            scoredVictoryPoints <= 0 ||
            !currentPlayer.HasKeepCard(KnownCardIds.FreezeTime))
        {
            return;
        }

        gameState.ScheduleExtraTurnAfterCurrentPlayer(currentPlayer.PlayerId, KnownCardIds.FreezeTime, -1);
    }

    private static int RemoveStatusTokensWithHearts(
        PlayerState currentPlayer,
        int heartCount,
        List<GameEventBase> newEvents)
    {
        var heartsRemaining = heartCount;

        while (heartsRemaining > 0 && currentPlayer.Status.PoisonTokens > 0)
        {
            currentPlayer.Status.RemovePoisonTokens(1);
            heartsRemaining--;

            newEvents.Add(new StatusTokensRemovedEvent(currentPlayer.PlayerId, 1, 0));
        }

        while (heartsRemaining > 0 && currentPlayer.Status.ShrinkTokens > 0)
        {
            currentPlayer.Status.RemoveShrinkTokens(1);
            heartsRemaining--;

            newEvents.Add(new StatusTokensRemovedEvent(currentPlayer.PlayerId, 0, 1));
        }

        return heartsRemaining;
    }
}
