using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class RapidHealingOutOfTurnFlowTests
{
    [Fact]
    public void OtherPlayer_CanHealDuringActivePlayersTurn()
    {
        var active = new PlayerState(0, "Active");
        var healer = new PlayerState(1, "Healer");
        healer.AddKeepCard(new MarketCardState(KnownCardIds.RapidHealing, "Rapid Healing", "Heal anytime.", 3, MarketCardType.Keep));
        healer.GainEnergy(2);
        healer.TakeDamage(2);
        var game = new GameState(new[] { active, healer }, new GameOptions(2));
        var engine = new GameEngine();
        engine.Execute(game, new InitializeGameCommand());
        engine.Execute(game, new BeginTurnCommand(0));
        engine.Execute(game, new RollDiceCommand(0));
        var pending = game.PendingDecision;

        var result = engine.Execute(game, new ActivateRapidHealingCommand(1));

        Assert.True(result.Success, result.Error);
        Assert.Equal(9, healer.Health);
        Assert.Equal(0, healer.Energy);
        Assert.Equal(0, game.CurrentTurn!.CurrentPlayerId);
        Assert.Same(pending, result.PendingDecision);
        Assert.Same(pending, game.PendingDecision);
    }
}
