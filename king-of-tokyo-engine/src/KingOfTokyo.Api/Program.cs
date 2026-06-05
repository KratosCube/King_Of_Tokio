using KingOfTokyo.Api.Endpoints;
using KingOfTokyo.Api.GameSessions;
using KingOfTokyo.Api.Lobbies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalBlazorDevelopment", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();
builder.Services.AddSingleton<ILobbyStore, InMemoryLobbyStore>();

var app = builder.Build();

app.UseCors("LocalBlazorDevelopment");

app.MapKingOfTokyoGameEndpoints();
app.MapKingOfTokyoLobbyEndpoints();

app.Run();
