using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Api.Lobbies;
using KingOfTokyo.Core.Commands;
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

        lobbies.MapGet("/", (bool? publicOnly, [FromServices] ILobbyStore store) =>
        {
            var result = store.ListLobbies(publicOnly ?? true);
            return Results.Ok(result);
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

        lobbies.MapPost("/{lobbyId:guid}/leave", (Guid lobbyId, LeaveLobbyRequest request, [FromServices] ILobbyStore store) =>
        {
            var found = store.TryLeaveLobby(lobbyId, request, out var result, out var error);
            if (!found)
            {
                return Results.NotFound(new { error = "Lobby was not found." });
            }

            return error is null
                ? Results.Ok(result)
                : Results.BadRequest(new { error });
        });

        lobbies.MapPost("/{lobbyId:guid}/start", (
            Guid lobbyId,
            StartLobbyRequest request,
            [FromServices] ILobbyStore lobbyStore,
            [FromServices] IGameSessionStore gameSessionStore) =>
        {
            var found = lobbyStore.TryPrepareStart(lobbyId, request, out var preparation, out var error);
            if (!found)
            {
                return Results.NotFound(new { error = "Lobby was not found." });
            }

            if (error is not null || preparation is null)
            {
                return Results.BadRequest(new { error });
            }

            var game = gameSessionStore.CreateGame(preparation.GameRequest);
            if (!gameSessionStore.TryExecute(game.GameId, (engine, state) =>
                {
                    var initializeResult = engine.Execute(state, new InitializeGameCommand());
                    return !initializeResult.Success
                        ? initializeResult
                        : engine.Execute(state, new BeginTurnCommand());
                }, out var autoStartResult) || autoStartResult is null)
            {
                return Results.NotFound(new { error = "Game was not found after creation." });
            }

            if (!autoStartResult.Success)
            {
                return Results.BadRequest(new { error = autoStartResult.Error ?? "Game could not be initialized." });
            }

            game = autoStartResult.GameState;

            var attached = lobbyStore.TryAttachGame(lobbyId, game.GameId, out var startedLobby, out var attachError);
            if (!attached)
            {
                return Results.NotFound(new { error = "Lobby was not found." });
            }

            return attachError is null && startedLobby is not null
                ? Results.Ok(new LobbyStartResultDto(startedLobby, game))
                : Results.BadRequest(new { error = attachError });
        });

        return endpoints;
    }
}
