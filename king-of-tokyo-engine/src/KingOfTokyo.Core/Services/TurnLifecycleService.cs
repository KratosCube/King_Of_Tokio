using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Victory;

namespace KingOfTokyo.Core.Services;

public sealed class TurnLifecycleService
{
    private readonly VictoryResolver _victoryResolver;
    private readonly KeepCardRulesService _keepCardRulesService;

    public TurnLifecycleService(
        VictoryResolver? victoryResolver = null,
        KeepCardRulesService? keepCardRulesService = null)
    {
        _victoryResolver = victoryResolver ?? new VictoryResolver();
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public EngineStepResult EndTurn(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var currentTurn = gameState.CurrentTurn
            ?? throw new InvalidOperationException("Cannot end turn without an active turn.");

        var currentPlayer = gameState.GetCurrentPlayer();
        var newEvents = new List<GameEventBase>();

        var hasFewestVictoryPoints = gameState.GetAlivePlayers()
            .Min(player => player.VictoryPoints) == currentPlayer.VictoryPoints;

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
}