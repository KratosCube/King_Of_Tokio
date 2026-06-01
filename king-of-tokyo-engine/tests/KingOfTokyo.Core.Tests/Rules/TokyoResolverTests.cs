using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Tokyo;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class TokyoResolverTests
{
    [Fact]
    public void EnterTokyo_Should_UseCity_WhenTokyoIsCompletelyEmpty()
    {
        var player = new PlayerState(0, "Monster");
        var gameState = new GameState(
            new[] { player, new PlayerState(1, "Other"), new PlayerState(2, "Other 2") },
            new GameOptions(3));

        var resolver = new TokyoResolver();

        var slot = resolver.EnterTokyo(gameState, player);

        Assert.Equal(TokyoSlot.City, slot);
        Assert.Equal(TokyoSlot.City, player.TokyoSlot);
        Assert.Equal(player.PlayerId, gameState.Tokyo.CityOccupantId);
    }

    [Fact]
    public void EnterTokyo_Should_UseBay_WhenCityIsOccupied_AndBayIsAvailable()
    {
        var cityPlayer = new PlayerState(0, "City");
        var bayPlayer = new PlayerState(1, "Bay");

        var others = new[]
        {
            cityPlayer,
            bayPlayer,
            new PlayerState(2, "Other"),
            new PlayerState(3, "Other 2"),
            new PlayerState(4, "Other 3")
        };

        var gameState = new GameState(others, new GameOptions(5));

        cityPlayer.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(cityPlayer.PlayerId);

        var resolver = new TokyoResolver();

        var slot = resolver.EnterTokyo(gameState, bayPlayer);

        Assert.Equal(TokyoSlot.Bay, slot);
        Assert.Equal(TokyoSlot.Bay, bayPlayer.TokyoSlot);
        Assert.Equal(bayPlayer.PlayerId, gameState.Tokyo.BayOccupantId);
    }

    [Fact]
    public void LeaveTokyo_Should_ClearCitySlot()
    {
        var player = new PlayerState(0, "Monster");
        var gameState = new GameState(
            new[] { player, new PlayerState(1, "Other"), new PlayerState(2, "Other 2") },
            new GameOptions(3));

        player.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(player.PlayerId);

        var resolver = new TokyoResolver();

        resolver.LeaveTokyo(gameState, player);

        Assert.Equal(TokyoSlot.None, player.TokyoSlot);
        Assert.Null(gameState.Tokyo.CityOccupantId);
    }
}