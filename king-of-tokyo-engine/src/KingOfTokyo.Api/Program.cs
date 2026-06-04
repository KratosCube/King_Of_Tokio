using KingOfTokyo.Api.Contracts;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Core.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryGameSessionStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var games = app.MapGroup("/api/games");

games.MapPost("/", (CreateGameRequest request, InMemoryGameSessionStore store) =>
{
    if (request.MonsterNames.Count is < 2 or > 6)
    {
        return Results.BadRequest(new { error = "Player count must be between 2 and 6." });
    }

    var snapshot = store.CreateGame(request.MonsterNames);
    return Results.Created($"/api/games/{snapshot.GameId}", snapshot);
});

games.MapGet("/{gameId:guid}", (Guid gameId, InMemoryGameSessionStore store) =>
{
    return store.TryGetSnapshot(gameId, out var snapshot)
        ? Results.Ok(snapshot)
        : Results.NotFound(new { error = "Game was not found." });
});

games.MapGet("/{gameId:guid}/events", (Guid gameId, long? after, InMemoryGameSessionStore store) =>
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

games.MapPost("/{gameId:guid}/commands/initialize", (Guid gameId, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new InitializeGameCommand()));
});

games.MapPost("/{gameId:guid}/commands/begin-turn", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new BeginTurnCommand(request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/roll-dice", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new RollDiceCommand(request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/reroll-dice", (Guid gameId, RerollDiceRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new RerollDiceCommand(request.DiceIndexesToReroll, request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/finalize-dice", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new FinalizeDiceCommand(request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/buy-face-up-card", (Guid gameId, BuyFaceUpCardRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new BuyFaceUpCardCommand(request.SlotIndex, request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/refresh-market", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new RefreshMarketCommand(request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/choose-leave-tokyo", (Guid gameId, ChooseLeaveTokyoRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new ChooseLeaveTokyoCommand(request.LeaveTokyo, request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/end-turn", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new EndTurnCommand(request.ActorPlayerId)));
});

games.MapPost("/{gameId:guid}/commands/advance-player", (Guid gameId, ActorRequest request, InMemoryGameSessionStore store) =>
{
    return Execute(gameId, store, (engine, state) => engine.Execute(state, new AdvanceToNextPlayerCommand(request.ActorPlayerId)));
});

app.Run();

static IResult Execute(
    Guid gameId,
    InMemoryGameSessionStore store,
    Func<KingOfTokyo.Core.Engine.GameEngine, KingOfTokyo.Core.Domain.State.GameState, KingOfTokyo.Core.Engine.CommandResult> execute)
{
    return store.TryExecute(gameId, execute, out var result)
        ? Results.Ok(result)
        : Results.NotFound(new { error = "Game was not found." });
}
