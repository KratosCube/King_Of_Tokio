# API layer plan

This document tracks the first server/API layer over the finished headless engine.

Primary validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current phase

Current phase: lobby/session backend stabilization before UI work.

The headless engine is complete for v1 scope. The API layer now acts as a thin adapter around the engine so a future web UI can create games, manage lobbies, send commands, read snapshots, and read incremental event cursors over HTTP.

## Current API project

Project:

```text
src/KingOfTokyo.Api/KingOfTokyo.Api.csproj
```

Current test project:

```text
tests/KingOfTokyo.Api.Tests/KingOfTokyo.Api.Tests.csproj
```

## Current architecture

```text
HTTP endpoint
  -> lobby/session endpoint layer
    -> ILobbyStore / IGameSessionStore
      -> GameState + GameEngine
        -> Core engine command handling
```

Current game session store implementation:

```text
src/KingOfTokyo.Api/GameSessions/InMemoryGameSessionStore.cs
```

Current lobby store implementation:

```text
src/KingOfTokyo.Api/Lobbies/InMemoryLobbyStore.cs
```

Store abstractions:

```text
src/KingOfTokyo.Api/GameSessions/IGameSessionStore.cs
src/KingOfTokyo.Api/Lobbies/ILobbyStore.cs
```

The endpoint layer depends on store interfaces, not directly on concrete in-memory implementations. This keeps the route handlers ready for a future persistent store, Redis-backed store, or authenticated lobby/session-aware store.

## Configurable game setup

The backend supports configurable starting health and target victory points.

Defaults:

```text
InitialHealth: 10
TargetVictoryPoints: 20
```

Current validation limits:

```text
InitialHealth: 1..50
TargetVictoryPoints: 1..100
```

These values are accepted by direct game creation and by lobby creation, and they are carried from lobby start into the created game session.

## Current endpoints

### Game/session endpoints

Base group:

```text
/api/games
```

```text
POST /api/games
GET  /api/games/{gameId}
GET  /api/games/{gameId}/events?after={eventSequence}
```

`POST /api/games` accepts:

```json
{
  "monsterNames": ["Alpha", "Beta"],
  "initialHealth": 10,
  "targetVictoryPoints": 20
}
```

### Core turn command endpoints

```text
POST /api/games/{gameId}/commands/initialize
POST /api/games/{gameId}/commands/begin-turn
POST /api/games/{gameId}/commands/roll-dice
POST /api/games/{gameId}/commands/reroll-dice
POST /api/games/{gameId}/commands/finalize-dice
POST /api/games/{gameId}/commands/buy-face-up-card
POST /api/games/{gameId}/commands/refresh-market
POST /api/games/{gameId}/commands/choose-leave-tokyo
POST /api/games/{gameId}/commands/end-turn
POST /api/games/{gameId}/commands/advance-player
```

### Special card / reaction endpoints

```text
POST /api/games/{gameId}/commands/activate-wings
POST /api/games/{gameId}/commands/activate-rapid-healing
POST /api/games/{gameId}/commands/activate-healing-ray
POST /api/games/{gameId}/commands/set-mimic-target
POST /api/games/{gameId}/commands/activate-telepath
POST /api/games/{gameId}/commands/activate-stretchy
POST /api/games/{gameId}/commands/activate-herd-culler
POST /api/games/{gameId}/commands/activate-smoke-cloud
POST /api/games/{gameId}/commands/activate-plot-twist
POST /api/games/{gameId}/commands/activate-metamorph
POST /api/games/{gameId}/commands/activate-psychic-probe
POST /api/games/{gameId}/commands/buy-owned-keep-card
POST /api/games/{gameId}/commands/peek-top-deck-card
POST /api/games/{gameId}/commands/buy-peeked-top-deck-card
POST /api/games/{gameId}/commands/decline-peeked-top-deck-card
POST /api/games/{gameId}/commands/buy-opportunist-revealed-card
```

### Lobby endpoints

Base group:

```text
/api/lobbies
```

```text
POST /api/lobbies
GET  /api/lobbies/{lobbyId}
POST /api/lobbies/{lobbyId}/join
POST /api/lobbies/{lobbyId}/ready
POST /api/lobbies/{lobbyId}/start
```

`POST /api/lobbies` accepts:

```json
{
  "name": "Friday kaiju night",
  "maxPlayers": 4,
  "isPublic": true,
  "hostDisplayName": "Host",
  "initialHealth": 10,
  "targetVictoryPoints": 20
}
```

Lobby start flow:

```text
host starts lobby
  -> ILobbyStore.TryPrepareStart
    -> CreateGameRequest from lobby seats/options
      -> IGameSessionStore.CreateGame
        -> ILobbyStore.TryAttachGame
          -> LobbyStartResultDto with LobbyDto + GameStateDto
```

## Important implementation notes

Minimal API route handlers use `[FromServices]` for store dependencies so ASP.NET resolves stores from DI instead of trying to infer them from request data.

`Program.cs` is intentionally small and only wires services plus endpoint registration.

Endpoint mapping lives in:

```text
src/KingOfTokyo.Api/Endpoints/GameEndpoints.cs
src/KingOfTokyo.Api/Endpoints/LobbyEndpoints.cs
```

## Current API tests

Current API coverage includes:

- in-memory session store tests,
- in-memory lobby store tests,
- lobby start preparation and attach-game tests,
- API command result DTO mapping tests,
- game endpoint route registration tests,
- lobby endpoint route registration tests.

The route registration tests intentionally do not test game rules. Game rules remain covered by `KingOfTokyo.Core.Tests`.

## Next steps before UI

### Immediate next step

Keep the full suite green after lobby start flow changes.

Command:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

### Remaining backend polish before UI

- add one higher-level test for lobby start creating a game session through both stores,
- decide whether player tokens remain client-held temporary tokens for the first UI prototype,
- document first UI screens and request/response examples,
- optionally add OpenAPI/Swagger if frontend development needs generated docs.

### Later server work

- persistence strategy for game sessions/lobbies,
- reconnect/resume flow,
- SignalR or polling strategy for event cursor updates,
- authentication/session identity,
- API integration tests through real HTTP once the endpoint contracts settle,
- production deployment concerns.
