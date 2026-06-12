using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KingOfTokyo.Api.Tests.Endpoints;

public sealed class GameEndpointsTests
{
    [Fact]
    public void MapKingOfTokyoGameEndpoints_Should_RegisterCoreGameRoutes()
    {
        using var app = CreateAppWithGameEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Contains("/api/games/", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/events", routePatterns);
    }

    [Fact]
    public void MapKingOfTokyoGameEndpoints_Should_RegisterDebugRoutes()
    {
        using var app = CreateAppWithGameEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Contains("/api/games/debug/cards", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/debug/grant-keep-card", routePatterns);
    }

    [Fact]
    public void MapKingOfTokyoGameEndpoints_Should_RegisterCoreTurnCommandRoutes()
    {
        using var app = CreateAppWithGameEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Contains("/api/games/{gameId:guid}/commands/initialize", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/begin-turn", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/roll-dice", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/reroll-dice", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/finalize-dice", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/buy-face-up-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/refresh-market", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/choose-leave-tokyo", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/end-turn", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/advance-player", routePatterns);
    }

    [Fact]
    public void MapKingOfTokyoGameEndpoints_Should_RegisterSpecialCardCommandRoutes()
    {
        using var app = CreateAppWithGameEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Contains("/api/games/{gameId:guid}/commands/activate-wings", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-rapid-healing", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-healing-ray", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/set-mimic-target", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-telepath", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-stretchy", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-herd-culler", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-smoke-cloud", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-plot-twist", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-metamorph", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/activate-psychic-probe", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/buy-owned-keep-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/peek-top-deck-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/buy-peeked-top-deck-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/decline-peeked-top-deck-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/buy-opportunist-revealed-card", routePatterns);
        Assert.Contains("/api/games/{gameId:guid}/commands/decline-opportunist-revealed-card", routePatterns);
    }

    [Fact]
    public void MapKingOfTokyoGameEndpoints_Should_RegisterExpectedNumberOfRoutes()
    {
        using var app = CreateAppWithGameEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Equal(32, routePatterns.Count);
    }

    private static WebApplication CreateAppWithGameEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();

        var app = builder.Build();
        app.MapKingOfTokyoGameEndpoints();

        return app;
    }

    private static IReadOnlySet<string> GetRoutePatterns(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }
}