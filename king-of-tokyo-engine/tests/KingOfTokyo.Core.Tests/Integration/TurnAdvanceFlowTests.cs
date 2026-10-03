using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class TurnAdvanceFlowTests
{
    [Fact]
    public void FinishedTurn_MustAdvanceExactlyOnceBeforeNextPlayerBegins()
    {
        var game = new GameState(new[] { new PlayerState(0, "First"), new PlayerState(1, "Second") }, new GameOptions(2));
        var engine = new GameEngine();
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        game.CurrentTurn!.MarkDiceResolved();
        game.CurrentTurn.SetPhase(TurnPhase.Purchase);
        Assert.True(engine.Execute(game, new EndTurnCommand(0)).Success);

        Assert.False(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.False(engine.Execute(game, new AdvanceToNextPlayerCommand(1)).Success);
        Assert.True(engine.Execute(game, new AdvanceToNextPlayerCommand(0)).Success);
        Assert.True(game.CurrentTurn.AdvancedToNextPlayer);
        Assert.True(game.ToDto().CurrentTurn!.AdvancedToNextPlayer);
        Assert.Equal(1, game.GetCurrentPlayer().PlayerId);

        Assert.False(engine.Execute(game, new AdvanceToNextPlayerCommand(0)).Success);
        Assert.Equal(1, game.GetCurrentPlayer().PlayerId);
        Assert.True(engine.Execute(game, new BeginTurnCommand(1)).Success);
        Assert.False(game.CurrentTurn!.AdvancedToNextPlayer);
    }
}
