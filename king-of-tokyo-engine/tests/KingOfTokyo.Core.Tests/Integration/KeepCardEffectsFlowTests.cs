using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class KeepCardEffectsFlowTests
{
    [Fact]
    public void BeginTurn_Should_UseFourRolls_WhenPlayerHasGiantBrain()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();

        currentPlayer.AddKeepCard(new MarketCardState(
            KnownCardIds.GiantBrain,
            "Giant Brain",
            "You have 1 extra reroll each turn.",
            5,
            MarketCardType.Keep));

        var engine = new GameEngine(marketSetupService: new MarketSetupService(shuffleDeck: false));

        engine.Execute(gameState, new InitializeGameCommand());
        var result = engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success);
        Assert.NotNull(gameState.CurrentTurn);
        Assert.Equal(4, gameState.CurrentTurn!.MaxRolls);
    }

    [Fact]
    public void BuyFaceUpCard_Should_CostOneLess_WhenPlayerHasAlienMetabolism()
    {
        var gameState = CreateGameState(4);
        var currentPlayer = gameState.GetCurrentPlayer();

        currentPlayer.AddKeepCard(new MarketCardState(
            KnownCardIds.AlienMetabolism,
            "Alien Metabolism",
            "Cards cost 1 less.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Heart,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(currentPlayer.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(currentPlayer.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(1, currentPlayer.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(0, currentPlayer.Energy);
        Assert.Single(gameState.Market.DiscardPile);
        Assert.Equal(KnownCardIds.Heal, gameState.Market.DiscardPile[0].CardId);
    }

    [Fact]
    public void FinalizeDice_Should_AddOneAttackDamage_WhenPlayerHasSpikedTail()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.SpikedTail,
            "Spiked Tail",
            "Your attacks deal 1 extra damage.",
            5,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
    }

    [Fact]
    public void FinalizeDice_Should_AddTwoVictoryPoints_WhenPlayerHasGourmet_AndScores()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Gourmet,
            "Gourmet",
            "When you score, gain 2 extra victory points.",
            4,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Energy, DieFace.Two);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(3, player.VictoryPoints);
    }

    [Fact]
    public void EndTurn_Should_GrantOneVictoryPoint_WhenPlayerHasHerbivore_AndDealtNoDamage()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Herbivore,
            "Herbivore",
            "At the end of each turn in which you dealt no damage, gain 1 victory point.",
            5,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Heart);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, player.VictoryPoints);
    }

    [Fact]
    public void FinalizeDice_Should_HealOneExtra_WhenPlayerHasRegeneration()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.TakeDamage(5);

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Regeneration,
            "Regeneration",
            "When you heal, heal 1 extra damage.",
            4,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Heart, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, player.Health);
    }

    [Fact]
    public void BuyHeal_Should_HealOneExtra_WhenPlayerHasRegeneration()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();
        player.TakeDamage(4);

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.Regeneration,
            "Regeneration",
            "When you heal, heal 1 extra damage.",
            4,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.Energy, DieFace.Energy, DieFace.Energy,
            DieFace.One, DieFace.Two, DieFace.Three);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(1, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(9, player.Health);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] sequence)
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)),
            marketSetupService: new MarketSetupService(shuffleDeck: false));
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
