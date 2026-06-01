using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;

namespace KingOfTokyo.Core.Services;

public sealed class RapidHealingService
{
    public const int ActivationCost = 2;
    public const int BaseHealing = 1;

    private readonly KeepCardRulesService _keepCardRulesService;

    public RapidHealingService(KeepCardRulesService? keepCardRulesService = null)
    {
        _keepCardRulesService = keepCardRulesService ?? new KeepCardRulesService();
    }

    public EngineStepResult Activate(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        var player = gameState.GetCurrentPlayer();

        player.SpendEnergy(ActivationCost);

        var bonusHealing = _keepCardRulesService.GetBonusHealing(player, BaseHealing);
        var totalHealing = BaseHealing + bonusHealing;

        var healthBefore = player.Health;
        player.Heal(totalHealing);
        var actualHealing = player.Health - healthBefore;

        if (actualHealing <= 0)
        {
            return EngineStepResult.Empty;
        }

        var events = new GameEventBase[]
        {
            new PlayerHealedEvent(
                player.PlayerId,
                actualHealing,
                bonusHealing > 0
                    ? "Rapid Healing + Regeneration."
                    : "Rapid Healing.")
        };

        return new EngineStepResult(events);
    }
}