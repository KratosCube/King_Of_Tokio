using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class ItHasAChildBayCleanupFlowTests
{
    [Fact]
    public void FinalizeDice_Should_KeepBayEnabledAndLetAttackerEnterBay_WhenBayOccupantIsRevivedByItHasAChildInFivePlayerGame()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        PutInCity(gameState, cityOccupant);
        PutInBay(gameState, bayOccupant);
        bayOccupant.TakeDamage(9);
        bayOccupant.AddKeepCard(CreateKeepCard(KnownCardIds.ItHasAChild, "It Has a Child", 7));
        bayOccupant.GainEnergy(4);
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.True(bayOccupant.IsAlive);
        Assert.Equal(10, bayOccupant.Health);
        Assert.Equal(10, bayOccupant.MaxHealth);
        Assert.Equal(0, bayOccupant.Energy);
        Assert.Empty(bayOccupant.KeepCards);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.Equal(TokyoSlot.Bay, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.BayOccupantId);
        Assert.True(gameState.Tokyo.BayEnabled);
        Assert.Equal(TokyoSlot.City, cityOccupant.TokyoSlot);
        Assert.Equal(cityOccupant.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Contains(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.ItHasAChild);
        Assert.Contains(result.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
                                             eliminated.EliminatedPlayerId == bayOccupant.PlayerId &&
                                             eliminated.EliminatedByPlayerId == attacker.PlayerId);
        Assert.Contains(result.NewEvents, e => e is TokyoEnteredEvent entered &&
                                             entered.PlayerId == attacker.PlayerId &&
                                             entered.Slot == TokyoSlot.Bay);
    }

    [Fact]
    public void FinalizeDice_Should_DisableBay_WhenBayOccupantIsRevivedButCityOccupantAlsoDiesLeavingFourAlive()
    {
        var gameState = CreateGameState(5);
        var attacker = gameState.GetCurrentPlayer();
        var cityOccupant = gameState.GetPlayerById(1);
        var bayOccupant = gameState.GetPlayerById(2);
        PutInCity(gameState, cityOccupant);
        PutInBay(gameState, bayOccupant);
        cityOccupant.TakeDamage(9);
        bayOccupant.TakeDamage(9);
        bayOccupant.AddKeepCard(CreateKeepCard(KnownCardIds.ItHasAChild, "It Has a Child", 7));
        var engine = CreateEngine(
            DieFace.Attack,
            DieFace.Heart,
            DieFace.One,
            DieFace.Two,
            DieFace.Three,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.False(cityOccupant.IsAlive);
        Assert.True(bayOccupant.IsAlive);
        Assert.Equal(10, bayOccupant.Health);
        Assert.Equal(TokyoSlot.None, bayOccupant.TokyoSlot);
        Assert.Null(gameState.Tokyo.BayOccupantId);
        Assert.False(gameState.Tokyo.BayEnabled);
        Assert.Equal(4, gameState.GetAlivePlayers().Count);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(attacker.PlayerId, gameState.Tokyo.CityOccupantId);
        Assert.Equal(2, result.NewEvents.OfType<PlayerEliminatedEvent>().Count());
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

    private static void PutInCity(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);
    }

    private static void PutInBay(GameState gameState, PlayerState player)
    {
        player.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetBayOccupant(player.PlayerId);
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
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
