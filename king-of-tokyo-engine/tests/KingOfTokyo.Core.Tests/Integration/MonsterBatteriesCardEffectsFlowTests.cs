using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MonsterBatteriesCardEffectsFlowTests
{
    [Fact]
    public void EndTurn_Should_DrainTwoStoredEnergyFromMonsterBatteries()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        var batteries = CreateMonsterBatteries(storedEnergy: 6);
        player.AddKeepCard(batteries);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(4, batteries.StoredEnergy);
        Assert.Contains(batteries, player.KeepCards);
        Assert.DoesNotContain(gameState.Market.DiscardPile, card => card.CardId == KnownCardIds.MonsterBatteries);
    }

    [Fact]
    public void EndTurn_Should_DiscardMonsterBatteries_WhenStoredEnergyReachesZero()
    {
        var gameState = CreateGameState(3);
        var player = gameState.GetCurrentPlayer();
        player.AddKeepCard(CreateMonsterBatteries(storedEnergy: 2));
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(player.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain(player.KeepCards, card => card.CardId == KnownCardIds.MonsterBatteries);
        Assert.Contains(gameState.Market.DiscardPile, card =>
            card.CardId == KnownCardIds.MonsterBatteries &&
            card.StoredEnergy == 0);
        Assert.Contains(result.NewEvents, e => e is KeepCardDiscardedEvent discarded &&
                                               discarded.PlayerId == player.PlayerId &&
                                               discarded.CardId == KnownCardIds.MonsterBatteries);
    }

    [Fact]
    public void EndTurn_Should_DrainMonsterBatteriesOwnedByAnyAlivePlayer()
    {
        var gameState = CreateGameState(3);
        var currentPlayer = gameState.GetCurrentPlayer();
        var otherPlayer = gameState.GetPlayerById(1);
        var currentPlayerBatteries = CreateMonsterBatteries(storedEnergy: 6);
        var otherPlayerBatteries = CreateMonsterBatteries(storedEnergy: 4);
        currentPlayer.AddKeepCard(currentPlayerBatteries);
        otherPlayer.AddKeepCard(otherPlayerBatteries);
        var engine = new GameEngine();

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(currentPlayer.PlayerId));

        var result = engine.Execute(gameState, new EndTurnCommand(currentPlayer.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(4, currentPlayerBatteries.StoredEnergy);
        Assert.Equal(2, otherPlayerBatteries.StoredEnergy);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static MarketCardState CreateMonsterBatteries(int storedEnergy)
    {
        return new MarketCardState(
            KnownCardIds.MonsterBatteries,
            "Monster Batteries",
            "Starts with 6 stored energy. At the end of each turn, lose 2 stored energy. Discard when empty.",
            5,
            MarketCardType.Keep,
            storedEnergy: storedEnergy);
    }
}
