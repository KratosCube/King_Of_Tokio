# Frontend stack decision

Decision: use Blazor WebAssembly for the first web UI prototype.

## Recommended stack

```text
Frontend: Blazor WebAssembly
Backend:  existing KingOfTokyo.Api ASP.NET Core Minimal API
Language: C# end-to-end
Styling:  CSS modules or plain scoped component CSS first
Realtime: polling first, SignalR later if needed
State:    small client-side state service first
```

Suggested project location:

```text
src/KingOfTokyo.Web
```

Suggested solution shape:

```text
src/KingOfTokyo.Core
src/KingOfTokyo.Api
src/KingOfTokyo.Web

tests/KingOfTokyo.Core.Tests
tests/KingOfTokyo.Api.Tests
```

## Why Blazor WebAssembly fits this project

The backend and engine are already C#/.NET. Blazor WebAssembly keeps the first UI prototype in the same language and toolchain as the engine and API.

Important project-specific advantages:

- one language across engine, API, and UI,
- easier DTO reuse or DTO mirroring,
- easy integration with existing Minimal API endpoints,
- good fit for a turn-based game where polling snapshots/events is acceptable,
- later SignalR support is natural if polling becomes too limited,
- less context switching during rapid iteration,
- easier for this repository to stay as one cohesive .NET solution.

## Why not React/Vue/Svelte first

React, Vue, or Svelte would also work and may offer a richer frontend ecosystem. But for this project, their main downside is adding a second language/toolchain and duplicating DTO/contracts in TypeScript.

They may become attractive later if:

- the UI becomes highly animation-heavy,
- a designer/frontend specialist joins and prefers TypeScript,
- component library needs become more important than C# end-to-end simplicity.

For the current prototype, Blazor is the lower-friction choice.

## Why not Blazor Server first

Blazor Server is quick to build, but the app would depend on a live server connection for UI interactivity. For an online board game UI, the cleaner first shape is a client app that talks to API endpoints and can later switch from polling to SignalR for updates.

Blazor Server can still be reconsidered for internal tooling or admin screens, but it should not be the main game client for the first prototype.

## First frontend milestone

The first UI milestone should validate the full online flow, not polish every card interaction.

Target flow:

```text
Create lobby
Join lobby in another browser tab
Guest ready
Host start
Both clients navigate to game table
Initialize game
Begin turn
Roll dice
Finalize dice
Resolve leave-Tokyo decision if needed
End turn
Advance player
Render event feed
```

## Initial UI screens

```text
/                         Home
/lobbies/new              Create lobby
/lobbies/{lobbyId}        Lobby room
/games/{gameId}           Game table
```

## Initial frontend services

Suggested services/classes:

```text
ApiClient
ClientSessionState
LobbyStateService
GameStateService
EventFeedService
```

### ApiClient

Wraps HTTP calls:

```text
POST /api/lobbies
GET  /api/lobbies/{lobbyId}
POST /api/lobbies/{lobbyId}/join
POST /api/lobbies/{lobbyId}/ready
POST /api/lobbies/{lobbyId}/start
GET  /api/games/{gameId}
GET  /api/games/{gameId}/events?after={sequence}
POST /api/games/{gameId}/commands/*
```

### ClientSessionState

Stores temporary local identity for the prototype:

```csharp
public sealed class ClientSessionState
{
    public Guid? LobbyId { get; set; }
    public Guid? GameId { get; set; }
    public int? PlayerId { get; set; }
    public Guid? PlayerToken { get; set; }
    public long LastEventSequence { get; set; }
}
```

For the first prototype, local browser storage is acceptable. Later this should be replaced or backed by real authentication/session identity.

## Polling strategy for first prototype

Lobby screen:

```text
GET /api/lobbies/{lobbyId}
every 1000ms to 2000ms
```

Game screen:

```text
GET /api/games/{gameId}
every 1500ms to 2500ms or after commands

GET /api/games/{gameId}/events?after={lastEventSequence}
every 750ms to 1500ms
```

This is good enough for the first prototype. Later we can move event updates to SignalR/SSE.

## Styling approach

Start simple:

```text
Razor components
scoped CSS files
no heavy UI framework initially
```

A component library can be added later if needed. The game table will likely need custom layout anyway.

Suggested early components:

```text
LobbyPlayerList
GamePlayerPanel
TokyoBoard
DicePanel
MarketPanel
CommandPanel
EventFeed
CardView
```

## Backend follow-ups after UI starts

These are not blockers for creating the Blazor prototype:

- token-validated command endpoints instead of raw ActorPlayerId,
- AvailableActionsDto for easier button enablement,
- real HTTP integration tests,
- OpenAPI/Swagger,
- persistence/reconnect,
- SignalR/SSE realtime updates.

## Immediate next implementation step

Create the Blazor WebAssembly project under:

```text
src/KingOfTokyo.Web
```

Then add it to:

```text
KingOfTokyo.Engine.slnx
```

First implementation slice:

1. create the project,
2. add basic routing,
3. add `ApiClient`,
4. add placeholder pages:
   - Home,
   - CreateLobby,
   - LobbyRoom,
   - GameTable,
5. verify build with:

```bash
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```
