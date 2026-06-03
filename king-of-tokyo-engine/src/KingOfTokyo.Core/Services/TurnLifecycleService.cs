using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Victory;

namespace KingOfTokyo.Core.Services;

public sealed class TurnLifecycleService
{
    private readonly VictoryResolver _victoryResolver;
    private readonly EliminationService _eliminationService;
    private readonly KeepCardRulesService _keepCardRulesService;

    public TurnLifecycleService(
        VictoryResolver? victoryResolver = null,
        KeepCardRulesService? keepCardRulesService = null,
        EliminationService? eliminationService = null)
    {
        _victoryResolver = victoryResolver ?? new VictoryResolver();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
        _eliminationService = eliminationService ?? new EliminationService();
    }

    public EngineStepResult EndTurn(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot end turn without an active turn.");

        var currentPlayer = gameState.GetCurrentPlayer();
        var newEvents = new List<GameEventBase>();

        var poisonDamage = currentPlayer.Status.PoisonTokens;
        if (poisonDamage > 0)
        {
            var healthBefore = currentPlayer.Health;
            currentPlayer.TakeDamage(poisonDamage);
            var actualDamage = healthBefore - currentPlayer.Health;

            if (actualDamage > 0)
            {
                newEvents.Add(new DamageDealtEvent(
                    currentPlayer.PlayerId,
                    currentPlayer.PlayerId,
                    actualDamage,
                    DamageKind.StatusEffect));
            }

            if (!currentPlayer.IsAlive && _eliminationService.TryEliminate(gameState, currentPlayer))
            {
                currentTurn.Flags.EliminatedSomeone = true;

                newEvents.Add(new PlayerEliminatedEvent(
                    currentPlayer.PlayerId,
                    currentPlayer.PlayerId,
                    "Poison tokens."));

                AwardEaterOfTheDeadPoints(gameState, newEvents);
            }
        }

        var hasFewestVictoryPoints = gameState.GetAlivePlayers().Count > 0 &&
            gameState.GetAlivePlayers().Min(player => player.VictoryPoints) == currentPlayer.VictoryPoints;

        if (currentPlayer.IsAlive)
        {
            var solarPoweredEnergy = _keepCardRulesService.GetEndTurnEnergyGainWhenEmpty(currentPlayer);
            if (solarPoweredEnergy > 0)
            {
                currentPlayer.GainEnergy(solarPoweredEnergy);

                newEvents.Add(new EnergyGainedEvent(
                    currentPlayer.PlayerId,
                    solarPoweredEnergy,
                    "Keep card: Solar Powered."));
            }

            var energyHoarderPoints = _keepCardRulesService.GetEndTurnVictoryPointsFromStoredEnergy(currentPlayer);
            if (energyHoarderPoints > 0)
            {
                currentPlayer.GainVictoryPoints(energyHoarderPoints);
                currentTurn.Flags.ScoredVictoryPoints = true;

                newEvents.Add(new VictoryPointsGainedEvent(
                    currentPlayer.PlayerId,
                    energyHoarderPoints,
                    "Keep card: Energy Hoarder."));
            }

            var underdogPoints = _keepCardRulesService.GetEndTurnUnderdogVictoryPoints(
                currentPlayer,
                hasFewestVictoryPoints);

            if (underdogPoints > 0)
            {
                currentPlayer.GainVictoryPoints(underdogPoints);
                currentTurn.Flags.ScoredVictoryPoints = true;

                newEvents.Add(new VictoryPointsGainedEvent(
                    currentPlayer.PlayerId,
                    underdogPoints,
                    "Keep card: Rooting for the Underdog."));
            }

            var herbivoreBonus = _keepCardRulesService.GetEndTurnVictoryPoints(
                currentPlayer,
                currentTurn.Flags.DealtDamage);

            if (herbivoreBonus > 0)
            {
                currentPlayer.GainVictoryPoints(herbivoreBonus);
                currentTurn.Flags.ScoredVictoryPoints = true;

                newEvents.Add(new VictoryPointsGainedEvent(
                    currentPlayer.PlayerId,
                    herbivoreBonus,
                    "Keep card: Herbivore."));
            }

            DrainMonsterBatteries(gameState, newEvents);
        }

        currentTurn.MarkPurchasePhaseFinished();
        currentTurn.SetPhase(TurnPhase.TurnEnd);

        var winnerInfo = _victoryResolver.Resolve(gameState);

        if (winnerInfo is not null)
        {
            gameState.FinishGame(winnerInfo);

            newEvents.Add(new GameEndedEvent(
                winnerInfo.WinnerPlayerId,
                winnerInfo.Reason ?? "Game ended."));

            return new EngineStepResult(newEvents);
        }

        currentTurn.SetPhase(TurnPhase.Finished);
        gameState.ClearPendingDecision();

        return newEvents.Count == 0
            ? EngineStepResult.Empty
            : new EngineStepResult(newEvents);
    }

    public void AdvanceToNextPlayer(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        gameState.AdvanceToNextAlivePlayer();
    }

    private static void DrainMonsterBatteries(GameState gameState, List<GameEventBase> newEvents)
    {
        foreach (var player in gameState.GetAlivePlayers().ToArray())
        {
            foreach (var battery in player.KeepCards
                         .Where(card => card.CardId == KnownCardIds.MonsterBatteries && card.StoredEnergy > 0)
                         .ToArray())
            {
                battery.SpendStoredEnergy(Math.Min(2, battery.StoredEnergy));

                if (battery.StoredEnergy > 0)
                {
                    continue;
                }

                var discardedCard = player.RemoveKeepCard(KnownCardIds.MonsterBatteries);
                new MimicTargetCleanupService().ClearTargetsForLostCard(gameState, player.PlayerId, discardedCard.CardId);
                gameState.Market.Discard(discardedCard);

                newEvents.Add(new KeepCardDiscardedEvent(
                    player.PlayerId,
                    discardedCard.CardId,
                    discardedCard.Name,
                    "Keep card: Monster Batteries."));
            }
        }
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
}
