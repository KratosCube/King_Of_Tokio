using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Api.Lobbies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();
builder.Services.AddSingleton<ILobbyStore, InMemoryLobbyStore>();

var app = builder.Build();

app.MapKingOfTokyoGameEndpoints();
app.MapKingOfTokyoLobbyEndpoints();

app.Run();
