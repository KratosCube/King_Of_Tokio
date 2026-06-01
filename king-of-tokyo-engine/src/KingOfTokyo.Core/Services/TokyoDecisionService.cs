using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Attack;
using KingOfTokyo.Core.Rules.Tokyo;
using KingOfTokyo.Core.Rules.Victory;

namespace KingOfTokyo.Core.Services;

public sealed class TokyoDecisionService
{
    private readonly TokyoResolver _tokyoResolver;
    private readonly DamageApplier _damageApplier;
    private readonly EliminationService _eliminationService;
    private readonly KeepCardRulesService _keepCardRulesService;

    public TokyoDecisionService(
        TokyoResolver? tokyoResolver = null,
        DamageApplier? damageApplier = null,
        EliminationService? eliminationService = null,
        KeepCardRulesService? keepCardRulesService = null)
    {
        _tokyoResolver = tokyoResolver ?? new TokyoResolver();
        _damageApplier = damageApplier ?? new DamageApplier();
        _eliminationService = eliminationService ?? new EliminationService();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public EngineStepResult Execute(GameState gameState, bool leaveTokyo)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot resolve Tokyo decision without an active turn.");

        var context = currentTurn.DequeueTokyoLeaveDecision();

        var attacker = gameState.GetPlayerById(context.AttackerPlayerId);
        var defender = gameState.GetPlayerById(context.DefenderPlayerId);

        var newEvents = new List<GameEventBase>();

        if (leaveTokyo && defender.IsAlive && defender.TokyoSlot != TokyoSlot.None)
        {
            var previousSlot = defender.TokyoSlot;
            var defenderHasBurrowing = _keepCardRulesService.HasBurrowing(defender);

            var jetsHealing = _keepCardRulesService.GetHealingWhenLeavingTokyo(defender, context.DamageTaken);
            if (jetsHealing > 0)
            {
                var healthBefore = defender.Health;
                defender.Heal(jetsHealing);
                var actualHealing = defender.Health - healthBefore;

                if (actualHealing > 0)
                {
                    newEvents.Add(new PlayerHealedEvent(
                        defender.PlayerId,
                        actualHealing,
                        "Keep card: Jets."));
                }
            }

            _tokyoResolver.LeaveTokyo(gameState, defender);
            newEvents.Add(new TokyoLeftEvent(defender.PlayerId, previousSlot));

            if (attacker.IsAlive && attacker.TokyoSlot == TokyoSlot.None)
            {
                var enteredSlot = _tokyoResolver.EnterTokyo(gameState, attacker);
                attacker.GainVictoryPoints(1);
                currentTurn.Flags.EnteredTokyo = true;
                currentTurn.Flags.ScoredVictoryPoints = true;

                newEvents.Add(new TokyoEnteredEvent(attacker.PlayerId, enteredSlot));
                newEvents.Add(new VictoryPointsGainedEvent(
                    attacker.PlayerId,
                    1,
                    "Entered Tokyo after defender left."));

                if (defenderHasBurrowing)
                {
                    var packet = new DamagePacket
                    {
                        SourcePlayerId = defender.PlayerId,
                        TargetPlayerId = attacker.PlayerId,
                        Amount = 1,
                        DamageKind = DamageKind.CardEffect,
                        CountsAsAttack = false,
                        AllowsTokyoLeave = false
                    };

                    var actualDamage = _damageApplier.ApplyDamage(attacker, packet);
                    if (actualDamage > 0)
                    {
                        newEvents.Add(new DamageDealtEvent(
                            packet.SourcePlayerId,
                            packet.TargetPlayerId,
                            actualDamage,
                            packet.DamageKind));

                        if (!attacker.IsAlive && _eliminationService.TryEliminate(gameState, attacker))
                        {
                            currentTurn.Flags.EliminatedSomeone = true;

                            newEvents.Add(new PlayerEliminatedEvent(
                                attacker.PlayerId,
                                defender.PlayerId,
                                "Keep card: Burrowing."));

                            AwardEaterOfTheDeadPoints(gameState, newEvents);
                        }
                    }
                }
            }
        }

        PendingDecision? nextPendingDecision = null;

        if (currentTurn.HasPendingTokyoLeaveDecisions)
        {
            nextPendingDecision = CreateTokyoLeavePendingDecision(currentTurn.PeekTokyoLeaveDecision());
            gameState.SetPendingDecision(nextPendingDecision);
        }
        else
        {
            currentTurn.SetPhase(TurnPhase.Purchase);
            gameState.ClearPendingDecision();
        }

        return new EngineStepResult(newEvents, nextPendingDecision);
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