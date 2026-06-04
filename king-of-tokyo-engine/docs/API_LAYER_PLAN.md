# API layer plan

This document tracks the first server/API layer over the finished headless engine.

Primary validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current phase

Current phase: API skeleton / server adapter stabilization.

The headless engine is complete for v1 scope. The API layer now acts as a thin adapter around the engine so a future web UI can create games, send commands, read snapshots, and read incremental event cursors over HTTP.

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
  -> IGameSessionStore
    -> GameState + GameEngine
      -> Core engine command handling
```

Current store implementation:

```text
src/KingOfTokyo.Api/GameSessions/InMemoryGameSessionStore.cs
```

Store abstraction:

```text
src/KingOfTokyo.Api/GameSessions/IGameSessionStore.cs
```

The endpoint layer depends on `IGameSessionStore`, not directly on `InMemoryGameSessionStore`. This keeps the route handlers ready for a future persistent store, Redis-backed store, or lobby/session-aware store.

## Current endpoints

Base group:

```text
/api/games
```

Game/session endpoints:

```text
POST /api/games
GET  /api/games/{gameId}
GET  /api/games/{gameId}/events?after={eventSequence}
```

Core turn command endpoints:

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

Special card / reaction endpoints:

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

## Important implementation notes

Minimal API route handlers use `[FromServices] IGameSessionStore store` so ASP.NET resolves the store from DI instead of trying to infer it from request data.

`Program.cs` is intentionally small and only wires services plus endpoint registration.

Endpoint mapping lives in:

```text
src/KingOfTokyo.Api/Endpoints/GameEndpoints.cs
```

## Current API tests

Current API coverage includes:

- in-memory session store tests,
- API command result DTO mapping tests,
- endpoint route registration tests.

The route registration tests intentionally do not test game rules. Game rules remain covered by `KingOfTokyo.Core.Tests`.

## Next steps

### Immediate next step

Keep the full suite green after `IGameSessionStore` and `[FromServices]` changes.

Command:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

### Next API work block

Add a small lobby/session model:

- create public/private game session metadata,
- join game,
- assign seats/player ids,
- track display names,
- readiness/start game flow,
- prevent commands from non-seated players once auth/session identity exists.

### Later server work

- persistence strategy for game sessions,
- reconnect/resume flow,
- SignalR or polling strategy for event cursor updates,
- authentication/session identity,
- API integration tests through real HTTP once the endpoint contracts settle,
- OpenAPI/Swagger if useful for frontend development.
