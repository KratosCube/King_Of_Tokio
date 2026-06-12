using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MoreKeepCardEffectsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GainOneVictoryPoint_WhenPlayerHasAlphaMonster_AndAttacks()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.AlphaMonster,
            "Alpha Monster",
            "Gain 1 victory point when you attack.",
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
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == attacker.PlayerId &&
                                               gained.Amount == 1);
    }

    [Fact]
    public void BuyFaceUpCard_Should_GainOneVictoryPoint_WhenPlayerHasDedicatedNewsTeam()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.DedicatedNewsTeam,
            "Dedicated News Team",
            "Whenever you buy a card, gain 1 victory point.",
            3,
            MarketCardType.Keep));

        player.GainEnergy(3);

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Heart, DieFace.Two);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new BuyFaceUpCardCommand(1, player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 1);
    }

    [Fact]
    public void FinalizeDice_Should_GainThreeVictoryPoints_ForAliveOwnerOfEaterOfTheDead_WhenAnotherMonsterDies()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);
        var observer = gameState.GetPlayerById(2);

        observer.AddKeepCard(new MarketCardState(
            KnownCardIds.EaterOfTheDead,
            "Eater of the Dead",
            "Gain 3 victory points whenever any monster reaches 0 health.",
            4,
            MarketCardType.Keep));

        defender.TakeDamage(9);
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
        Assert.False(defender.IsAlive);
        Assert.Equal(3, observer.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == observer.PlayerId &&
                                               gained.Amount == 3);
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
