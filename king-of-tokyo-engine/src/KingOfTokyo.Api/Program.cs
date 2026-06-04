using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();

var app = builder.Build();

app.MapKingOfTokyoGameEndpoints();

app.Run();
