using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MimicActivatedAbilityFlowTests
{
    [Fact]
    public void CanUseHerdCuller_Should_ReturnTrue_WhenMimicCopiesHerdCuller()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.HerdCuller, "Herd Culler"));
        var service = new KeepCardRulesService();

        var canUseHerdCuller = service.CanUseHerdCuller(player);

        Assert.True(canUseHerdCuller);
    }

    [Fact]
    public void CanUseTelepath_Should_ReturnTrue_WhenMimicCopiesTelepathAndPlayerHasEnergy()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Telepath, "Telepath"));
        player.GainEnergy(1);
        var service = new KeepCardRulesService();

        var canUseTelepath = service.CanUseTelepath(player);

        Assert.True(canUseTelepath);
    }

    [Fact]
    public void CanUseStretchy_Should_ReturnTrue_WhenMimicCopiesStretchyAndPlayerHasEnergy()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Stretchy, "Stretchy"));
        player.GainEnergy(2);
        var service = new KeepCardRulesService();

        var canUseStretchy = service.CanUseStretchy(player);

        Assert.True(canUseStretchy);
    }

    [Fact]
    public void ActivateTelepath_Should_Work_WhenPlayerMimicsTelepath()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Telepath, "Telepath"));
        player.GainEnergy(1);

        var engine = CreateEngine(
            DieFace.Energy, DieFace.One, DieFace.Two,
            DieFace.Three, DieFace.Heart, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateTelepathCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, player.Energy);
        Assert.Equal(4, gameState.CurrentTurn!.MaxRolls);
        Assert.NotNull(gameState.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, gameState.PendingDecision!.DecisionType);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.Telepath));
    }

    [Fact]
    public void ActivateStretchy_Should_Work_WhenPlayerMimicsStretchy()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.Stretchy, "Stretchy"));
        player.GainEnergy(2);

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.One,
            DieFace.Two, DieFace.Heart, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateStretchyCommand(2, DieFace.Attack, player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(0, player.Energy);
        Assert.Equal(DieFace.Attack, gameState.CurrentTurn!.DicePool.Dice[2].CurrentFace);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.Stretchy));
    }

    [Fact]
    public void ActivateHerdCuller_Should_Work_WhenPlayerMimicsHerdCuller()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.HerdCuller, "Herd Culler"));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Attack,
            DieFace.Two, DieFace.Heart, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new ActivateHerdCullerCommand(2, player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(DieFace.One, gameState.CurrentTurn!.DicePool.Dice[2].CurrentFace);
        Assert.True(gameState.CurrentTurn.Flags.HerdCullerUsed);
        Assert.True(player.HasKeepCard(KnownCardIds.Mimic));
        Assert.False(player.HasKeepCard(KnownCardIds.HerdCuller));
    }

    [Fact]
    public void ActivateHerdCuller_Should_StillBeOncePerTurn_WhenPlayerMimicsHerdCuller()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMimicCopying(KnownCardIds.HerdCuller, "Herd Culler"));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Attack,
            DieFace.Two, DieFace.Heart, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new ActivateHerdCullerCommand(2, player.PlayerId));

        var result = engine.Execute(gameState, new ActivateHerdCullerCommand(3, player.PlayerId));

        Assert.False(result.Success);
        Assert.Equal("Herd Culler can only be used once per turn.", result.Error);
        Assert.Equal(DieFace.Two, gameState.CurrentTurn!.DicePool.Dice[3].CurrentFace);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] faces)
    {
        return new GameEngine(diceRollService: new DiceRollService(new SequenceRandomSource(faces)));
    }

    private static MarketCardState CreateMimicCopying(string copiedCardId, string copiedCardName)
    {
        return new MarketCardState(
            KnownCardIds.Mimic,
            "Mimic",
            "Copy another keep card.",
            8,
            MarketCardType.Keep,
            mimicTarget: new MimicTargetState(1, copiedCardId, copiedCardName));
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(IEnumerable<DieFace> faces)
        {
            _faces = new Queue<DieFace>(faces);
        }

        public DieFace RollDieFace()
        {
            if (_faces.Count == 0)
            {
                throw new InvalidOperationException("No more queued die faces in SequenceRandomSource.");
            }

            return _faces.Dequeue();
        }
    }
}
