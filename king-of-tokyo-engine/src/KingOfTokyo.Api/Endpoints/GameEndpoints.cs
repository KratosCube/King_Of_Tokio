using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Engine;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTokyo.Api.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapKingOfTokyoGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var games = endpoints.MapGroup("/api/games");
        games.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var store = http.RequestServices.GetRequiredService<IGameSessionStore>();
            if (!Guid.TryParse(http.Request.RouteValues["gameId"]?.ToString(), out var gameId) ||
                !Guid.TryParse(http.Request.Headers["X-Player-Token"].ToString(), out var token) ||
                !store.TryAuthorize(gameId, token, out var playerId))
            {
                return Results.Unauthorized();
            }

            if (http.Request.Path.Value?.Contains("/commands/", StringComparison.Ordinal) == true)
            {
                var isInitialize = http.Request.Path.Value.EndsWith("/initialize", StringComparison.Ordinal);
                var request = context.Arguments.FirstOrDefault(arg => arg?.GetType().GetProperty("ActorPlayerId") is not null);
                var claimedActor = request?.GetType().GetProperty("ActorPlayerId")?.GetValue(request);
                if ((isInitialize && playerId != 0) ||
                    (!isInitialize && (claimedActor is not int || !Equals(claimedActor, playerId))))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            return await next(context);
        });

        games.MapGet("/{gameId:guid}", (Guid gameId, [FromServices] IGameSessionStore store) =>
        {
            return store.TryGetSnapshot(gameId, out var snapshot)
                ? Results.Ok(snapshot)
                : Results.NotFound(new { error = "Game was not found." });
        });

        games.MapGet("/{gameId:guid}/events", (Guid gameId, long? after, [FromServices] IGameSessionStore store) =>
        {
            try
            {
                return store.TryGetEvents(gameId, after ?? 0, out var cursor)
                    ? Results.Ok(cursor)
                    : Results.NotFound(new { error = "Game was not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        games.MapPost("/{gameId:guid}/commands/initialize", (Guid gameId, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new InitializeGameCommand()));
        });

        games.MapPost("/{gameId:guid}/commands/begin-turn", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BeginTurnCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/roll-dice", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RollDiceCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/reroll-dice", (Guid gameId, RerollDiceRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RerollDiceCommand(request.DiceIndexesToReroll, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/reroll-background-dweller-threes", (Guid gameId, RerollDiceRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RerollBackgroundDwellerThreesCommand(request.DiceIndexesToReroll, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/finalize-dice", (Guid gameId, FinalizeDiceRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new FinalizeDiceCommand(request.ActorPlayerId, request.HeartsReservedForHealingRay)));
        });

        games.MapPost("/{gameId:guid}/commands/continue-after-rapid-healing", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ContinueAfterRapidHealingCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-face-up-card", (Guid gameId, BuyFaceUpCardRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyFaceUpCardCommand(request.SlotIndex, request.ActorPlayerId, request.StoredEnergyToDeposit)));
        });

        games.MapPost("/{gameId:guid}/commands/refresh-market", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RefreshMarketCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/choose-leave-tokyo", (Guid gameId, ChooseLeaveTokyoRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ChooseLeaveTokyoCommand(request.LeaveTokyo, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/end-turn", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new EndTurnCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/advance-player", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new AdvanceToNextPlayerCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-wings", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateWingsCommand(RequireActor(request.ActorPlayerId))));
        });

        games.MapPost("/{gameId:guid}/commands/activate-rapid-healing", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateRapidHealingCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-healing-ray", (Guid gameId, HealingRayRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateHealingRayCommand(request.TargetPlayerId, request.HealingAmount, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/set-mimic-target", (Guid gameId, SetMimicTargetRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new SetMimicTargetCommand(request.TargetOwnerPlayerId, request.TargetCardId, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-telepath", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateTelepathCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-stretchy", (Guid gameId, ChangeDieFaceRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateStretchyCommand(request.DieIndex, request.TargetFace, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-herd-culler", (Guid gameId, DieIndexRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateHerdCullerCommand(request.DieIndex, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-smoke-cloud", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateSmokeCloudCommand(RequireActor(request.ActorPlayerId))));
        });

        games.MapPost("/{gameId:guid}/commands/activate-plot-twist", (Guid gameId, ChangeDieFaceRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivatePlotTwistCommand(request.DieIndex, request.TargetFace, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-metamorph", (Guid gameId, MetamorphRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateMetamorphCommand(request.CardIdToDiscard, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-psychic-probe", (Guid gameId, PsychicProbeRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivatePsychicProbeCommand(request.ActorPlayerId, request.TargetDieIndex)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-owned-keep-card", (Guid gameId, BuyOwnedKeepCardRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyOwnedKeepCardCommand(request.SellerPlayerId, request.CardId, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/peek-top-deck-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new PeekTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-peeked-top-deck-card", (Guid gameId, BatteryPurchaseRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyPeekedTopDeckCardCommand(request.ActorPlayerId, request.StoredEnergyToDeposit)));
        });

        games.MapPost("/{gameId:guid}/commands/decline-peeked-top-deck-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new DeclinePeekedTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-opportunist-revealed-card", (Guid gameId, BatteryPurchaseRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyOpportunistRevealedCardCommand(request.ActorPlayerId, request.StoredEnergyToDeposit)));
        });

        games.MapPost("/{gameId:guid}/commands/decline-opportunist-revealed-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new DeclineOpportunistRevealedCardCommand(request.ActorPlayerId)));
        });

        return endpoints;
    }

    private static IResult Execute(
        Guid gameId,
        IGameSessionStore store,
        Func<GameEngine, KingOfTokyo.Core.Domain.State.GameState, CommandResult> execute)
    {
        try
        {
            return store.TryExecute(gameId, execute, out var result)
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Game was not found." });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static int RequireActor(int? actorPlayerId)
    {
        if (actorPlayerId is null)
        {
            throw new ArgumentException("Actor player id is required for this command.", nameof(actorPlayerId));
        }

        return actorPlayerId.Value;
    }
}
