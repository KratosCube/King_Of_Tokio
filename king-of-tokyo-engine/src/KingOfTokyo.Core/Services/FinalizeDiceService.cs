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

            var pendingDecision = CreateTokyoLeavePendingDecision(currentTurn.PeekTokyoLeaveDecision());
            gameState.SetPendingDecision(pendingDecision);

            return new EngineStepResult(newEvents, pendingDecision);
        }

        currentTurn.SetPhase(TurnPhase.Purchase);
        gameState.ClearPendingDecision();

        return new EngineStepResult(newEvents);
    }

    private void ApplyAttackStatusTokens(
        PlayerState currentPlayer,
        PlayerState target,
        int attackCount,
        List<GameEventBase> newEvents)
    {
        var poisonTokens = _keepCardRulesService.GetPoisonTokensToApply(currentPlayer, attackCount);
        var shrinkTokens = _keepCardRulesService.GetShrinkTokensToApply(currentPlayer, attackCount);

        if (poisonTokens <= 0 && shrinkTokens <= 0)
        {
            return;
        }

        if (poisonTokens > 0)
        {
            target.Status.AddPoisonTokens(poisonTokens);
        }

        if (shrinkTokens > 0)
        {
            target.Status.AddShrinkTokens(shrinkTokens);
        }

        newEvents.Add(new StatusTokensAddedEvent(
            currentPlayer.PlayerId,
            target.PlayerId,
            poisonTokens,
            shrinkTokens));
    }

    private static int RemoveStatusTokensWithHearts(
        PlayerState player,
        int heartCount,
        List<GameEventBase> newEvents)
    {
        if (heartCount <= 0)
        {
            return 0;
        }

        var heartsRemaining = heartCount;
        var poisonTokensRemoved = Math.Min(player.Status.PoisonTokens, heartsRemaining);
        if (poisonTokensRemoved > 0)
        {
            player.Status.RemovePoisonTokens(poisonTokensRemoved);
            heartsRemaining -= poisonTokensRemoved;
        }

        var shrinkTokensRemoved = Math.Min(player.Status.ShrinkTokens, heartsRemaining);
        if (shrinkTokensRemoved > 0)
        {
            player.Status.RemoveShrinkTokens(shrinkTokensRemoved);
            heartsRemaining -= shrinkTokensRemoved;
        }

        var heartsSpent = poisonTokensRemoved + shrinkTokensRemoved;
        if (heartsSpent > 0)
        {
            newEvents.Add(new StatusTokensRemovedEvent(
                player.PlayerId,
                poisonTokensRemoved,
                shrinkTokensRemoved,
                heartsSpent));
        }

        return heartsRemaining;
    }

    private void ApplyPoisonQuillsDamage(
        GameState gameState,
        PlayerState currentPlayer,
        TurnState currentTurn,
        List<GameEventBase> newEvents,
        DiceResolutionSummary summary)
    {
        var poisonDamage = _keepCardRulesService.GetPoisonQuillsDamage(currentPlayer, summary.OneCount);
        if (poisonDamage <= 0)
        {
            return;
        }

        var targets = ResolvePositionalTargets(gameState, currentPlayer);

        foreach (var target in targets)
        {
            var packet = new DamagePacket
            {
                SourcePlayerId = currentPlayer.PlayerId,
                TargetPlayerId = target.PlayerId,
                Amount = poisonDamage,
                DamageKind = DamageKind.CardEffect,
                CountsAsAttack = false,
                AllowsTokyoLeave = false
            };

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

            if (!target.IsAlive && _eliminationService.TryEliminate(gameState, target))
            {
                currentTurn.Flags.EliminatedSomeone = true;

                newEvents.Add(new PlayerEliminatedEvent(
                    target.PlayerId,
                    currentPlayer.PlayerId,
                    "Keep card: Poison Quills."));

                AwardEaterOfTheDeadPoints(gameState, newEvents);
            }
        }
    }

    private static IReadOnlyList<PlayerState> ResolvePositionalTargets(GameState gameState, PlayerState sourcePlayer)
    {
        return sourcePlayer.TokyoSlot == TokyoSlot.None
            ? gameState.Players
                .Where(p => p.IsAlive && p.PlayerId != sourcePlayer.PlayerId && p.TokyoSlot != TokyoSlot.None)
                .ToArray()
            : gameState.Players
                .Where(p => p.IsAlive && p.PlayerId != sourcePlayer.PlayerId && p.TokyoSlot == TokyoSlot.None)
                .ToArray();
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

    private static PendingDecision CreateTokyoLeavePendingDecision(TokyoLeaveDecisionContext context)
    {
        return new PendingDecision
        {
            DecisionType = DecisionType.LeaveTokyo,
            PlayerId = context.DefenderPlayerId,
            Payload = new LeaveTokyoDecisionData
            {
                AttackerPlayerId = context.AttackerPlayerId,
                DefenderPlayerId = context.DefenderPlayerId,
                DamageTaken = context.DamageTaken
            }
        };
    }
}