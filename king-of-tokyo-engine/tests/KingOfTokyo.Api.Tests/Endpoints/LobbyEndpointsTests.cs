using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Api.Lobbies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KingOfTokyo.Api.Tests.Endpoints;

public sealed class LobbyEndpointsTests
{
    [Fact]
    public void MapKingOfTokyoLobbyEndpoints_Should_RegisterLobbyRoutes()
    {
        using var app = CreateAppWithLobbyEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Contains("/api/lobbies/", routePatterns);
        Assert.Contains("/api/lobbies/{lobbyId:guid}", routePatterns);
        Assert.Contains("/api/lobbies/{lobbyId:guid}/join", routePatterns);
        Assert.Contains("/api/lobbies/{lobbyId:guid}/ready", routePatterns);
        Assert.Contains("/api/lobbies/{lobbyId:guid}/start", routePatterns);
    }

    [Fact]
    public void MapKingOfTokyoLobbyEndpoints_Should_RegisterExpectedNumberOfRoutes()
    {
        using var app = CreateAppWithLobbyEndpoints();

        var routePatterns = GetRoutePatterns(app);

        Assert.Equal(5, routePatterns.Count);
    }

    private static WebApplication CreateAppWithLobbyEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ILobbyStore, InMemoryLobbyStore>();
        builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();

        var app = builder.Build();
        app.MapKingOfTokyoLobbyEndpoints();

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
