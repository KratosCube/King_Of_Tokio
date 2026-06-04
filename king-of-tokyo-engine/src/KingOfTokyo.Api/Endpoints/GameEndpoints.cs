using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Api.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapKingOfTokyoGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var games = endpoints.MapGroup("/api/games");

        games.MapPost("/", (CreateGameRequest request, IGameSessionStore store) =>
        {
            if (request.MonsterNames.Count is < 2 or > 6)
            {
                return Results.BadRequest(new { error = "Player count must be between 2 and 6." });
            }

            var snapshot = store.CreateGame(request.MonsterNames);
            return Results.Created($"/api/games/{snapshot.GameId}", snapshot);
        });

        games.MapGet("/{gameId:guid}", (Guid gameId, IGameSessionStore store) =>
        {
            return store.TryGetSnapshot(gameId, out var snapshot)
                ? Results.Ok(snapshot)
                : Results.NotFound(new { error = "Game was not found." });
        });

        games.MapGet("/{gameId:guid}/events", (Guid gameId, long? after, IGameSessionStore store) =>
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

        games.MapPost("/{gameId:guid}/commands/initialize", (Guid gameId, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new InitializeGameCommand()));
        });

        games.MapPost("/{gameId:guid}/commands/begin-turn", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BeginTurnCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/roll-dice", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RollDiceCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/reroll-dice", (Guid gameId, RerollDiceRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RerollDiceCommand(request.DiceIndexesToReroll, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/finalize-dice", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new FinalizeDiceCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-face-up-card", (Guid gameId, BuyFaceUpCardRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyFaceUpCardCommand(request.SlotIndex, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/refresh-market", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new RefreshMarketCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/choose-leave-tokyo", (Guid gameId, ChooseLeaveTokyoRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ChooseLeaveTokyoCommand(request.LeaveTokyo, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/end-turn", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new EndTurnCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/advance-player", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new AdvanceToNextPlayerCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-wings", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateWingsCommand(RequireActor(request.ActorPlayerId))));
        });

        games.MapPost("/{gameId:guid}/commands/activate-rapid-healing", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateRapidHealingCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-healing-ray", (Guid gameId, HealingRayRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateHealingRayCommand(request.TargetPlayerId, request.HealingAmount, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/set-mimic-target", (Guid gameId, SetMimicTargetRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new SetMimicTargetCommand(request.TargetOwnerPlayerId, request.TargetCardId, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-telepath", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateTelepathCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-stretchy", (Guid gameId, ChangeDieFaceRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateStretchyCommand(request.DieIndex, request.TargetFace, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-herd-culler", (Guid gameId, DieIndexRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateHerdCullerCommand(request.DieIndex, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-smoke-cloud", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateSmokeCloudCommand(RequireActor(request.ActorPlayerId))));
        });

        games.MapPost("/{gameId:guid}/commands/activate-plot-twist", (Guid gameId, ChangeDieFaceRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivatePlotTwistCommand(request.DieIndex, request.TargetFace, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-metamorph", (Guid gameId, MetamorphRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivateMetamorphCommand(request.CardIdToDiscard, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/activate-psychic-probe", (Guid gameId, PsychicProbeRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new ActivatePsychicProbeCommand(request.ActorPlayerId, request.TargetDieIndex)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-owned-keep-card", (Guid gameId, BuyOwnedKeepCardRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyOwnedKeepCardCommand(request.SellerPlayerId, request.CardId, request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/peek-top-deck-card", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new PeekTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-peeked-top-deck-card", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyPeekedTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/decline-peeked-top-deck-card", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new DeclinePeekedTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-opportunist-revealed-card", (Guid gameId, ActorRequest request, IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyOpportunistRevealedCardCommand(request.ActorPlayerId)));
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
