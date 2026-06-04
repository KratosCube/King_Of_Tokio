using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryGameSessionStore>();

var app = builder.Build();

app.MapKingOfTokyoGameEndpoints();

app.Run();
