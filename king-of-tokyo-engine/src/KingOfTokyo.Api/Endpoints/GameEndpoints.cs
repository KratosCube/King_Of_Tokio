using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTokyo.Api.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapKingOfTokyoGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var games = endpoints.MapGroup("/api/games");

        games.MapPost("/", (CreateGameRequest request, [FromServices] IGameSessionStore store) =>
        {
            if (request.MonsterNames.Count is < 2 or > 6)
            {
                return Results.BadRequest(new { error = "Player count must be between 2 and 6." });
            }

            var snapshot = store.CreateGame(request);
            return Results.Created($"/api/games/{snapshot.GameId}", snapshot);
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

        games.MapGet("/debug/cards", () =>
        {
            var cards = MarketSetupService.BuildDefaultDeck()
                .Where(card => card.CardType == MarketCardType.Keep)
                .OrderBy(card => card.Name)
                .Select(card => new DebugCardOptionDto(card.CardId, card.Name, card.Description, card.Cost, card.CardType))
                .ToArray();

            return Results.Ok(cards);
        });

        games.MapPost("/{gameId:guid}/debug/grant-keep-card", (Guid gameId, DebugGrantKeepCardRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (_, state) =>
            {
                var player = state.GetPlayerById(request.TargetPlayerId);
                var template = MarketSetupService.BuildDefaultDeck()
                    .FirstOrDefault(card => string.Equals(card.CardId, request.CardId, StringComparison.OrdinalIgnoreCase));

                if (template is null)
                {
                    throw new InvalidOperationException("Debug card was not found in the default deck.");
                }

                if (template.CardType != MarketCardType.Keep)
                {
                    throw new InvalidOperationException("Debug card grant only supports keep cards.");
                }

                if (player.HasKeepCard(template.CardId))
                {
                    throw new InvalidOperationException($"{player.MonsterName} already owns {template.Name}.");
                }

                var clonedCard = CloneCard(template);
                player.AddKeepCard(clonedCard);

                ApplyDebugGainEffects(player, clonedCard);

                return CommandResult.Successful(state);
            });
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

        games.MapPost("/{gameId:guid}/commands/finalize-dice", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new FinalizeDiceCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-face-up-card", (Guid gameId, BuyFaceUpCardRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyFaceUpCardCommand(request.SlotIndex, request.ActorPlayerId)));
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
            return Execute(gameId, store, (engine, state) =>
            {
                var advanceResult = engine.Execute(state, new AdvanceToNextPlayerCommand(request.ActorPlayerId));
                return !advanceResult.Success || state.Status != KingOfTokyo.Core.Domain.Enums.GameStatus.Running
                    ? advanceResult
                    : engine.Execute(state, new BeginTurnCommand());
            });
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

        games.MapPost("/{gameId:guid}/commands/buy-peeked-top-deck-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyPeekedTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/decline-peeked-top-deck-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new DeclinePeekedTopDeckCardCommand(request.ActorPlayerId)));
        });

        games.MapPost("/{gameId:guid}/commands/buy-opportunist-revealed-card", (Guid gameId, ActorRequest request, [FromServices] IGameSessionStore store) =>
        {
            return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyOpportunistRevealedCardCommand(request.ActorPlayerId)));
        });

        return endpoints;
    }

    private static MarketCardState CloneCard(MarketCardState card)
    {
        return new MarketCardState(
            card.CardId,
            card.Name,
            card.Description,
            card.Cost,
            card.CardType,
            card.PurchaseEffect,
            card.Counters,
            card.StoredEnergy,
            card.MimicTarget is null ? null : new MimicTargetState(card.MimicTarget.OwnerPlayerId, card.MimicTarget.CardId, card.MimicTarget.CardName));
    }

    private static void ApplyDebugGainEffects(PlayerState player, MarketCardState card)
    {
        if (card.PurchaseEffect.IncreaseMaxHealth > 0)
        {
            player.IncreaseMaxHealth(card.PurchaseEffect.IncreaseMaxHealth);
        }

        if (card.PurchaseEffect.Heal > 0)
        {
            player.Heal(card.PurchaseEffect.Heal);
        }
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
