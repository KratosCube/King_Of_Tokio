using KingOfTokyo.Api.Lobbies;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTokyo.Api.Endpoints;

public static class LobbyEndpoints
{
    public static IEndpointRouteBuilder MapKingOfTokyoLobbyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var lobbies = endpoints.MapGroup("/api/lobbies");

        lobbies.MapPost("/", (CreateLobbyRequest request, [FromServices] ILobbyStore store) =>
        {
            try
            {
                var result = store.CreateLobby(request);
                return Results.Created($"/api/lobbies/{result.Lobby.LobbyId}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        lobbies.MapGet("/{lobbyId:guid}", (Guid lobbyId, [FromServices] ILobbyStore store) =>
        {
            return store.TryGetLobby(lobbyId, out var lobby)
                ? Results.Ok(lobby)
                : Results.NotFound(new { error = "Lobby was not found." });
        });

        lobbies.MapPost("/{lobbyId:guid}/join", (Guid lobbyId, JoinLobbyRequest request, [FromServices] ILobbyStore store) =>
        {
            var found = store.TryJoinLobby(lobbyId, request, out var result, out var error);
            if (!found)
            {
                return Results.NotFound(new { error = "Lobby was not found." });
            }

            return error is null
                ? Results.Ok(result)
                : Results.BadRequest(new { error });
        });

        lobbies.MapPost("/{lobbyId:guid}/ready", (Guid lobbyId, SetLobbyReadyRequest request, [FromServices] ILobbyStore store) =>
        {
            var found = store.TrySetReady(lobbyId, request, out var lobby, out var error);
            if (!found)
            {
                return Results.NotFound(new { error = "Lobby was not found." });
            }

            return error is null
                ? Results.Ok(lobby)
                : Results.BadRequest(new { error });
        });

        return endpoints;
    }
}
