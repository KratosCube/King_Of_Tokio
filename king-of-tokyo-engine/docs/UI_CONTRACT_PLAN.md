# UI contract plan

This document defines the first frontend/UI contract for the King of Tokyo web prototype.

The backend is currently ready enough for a first UI prototype:

- headless engine is complete for v1 scope,
- game session API exists,
- lobby API exists,
- lobby can start a real game session,
- game snapshots are available through DTOs,
- incremental events are available through the event cursor endpoint,
- custom `InitialHealth` and `TargetVictoryPoints` are supported.

Primary validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## UI phase goal

Build a first playable web UI over the current API without solving production infrastructure yet.

This first UI should prioritize:

1. creating and joining a lobby,
2. ready/start flow,
3. rendering the game state,
4. executing game commands,
5. polling snapshots/events,
6. keeping the UI simple enough to validate the full online game flow.

## Non-goals for the first UI prototype

These can wait until after the first playable UI exists:

- real authentication,
- persistent database storage,
- reconnect across server restarts,
- full SignalR realtime transport,
- ranked/public matchmaking,
- mobile polish,
- final visual identity,
- animations for every event.

The current `PlayerToken` model is acceptable for the first prototype. It can later be replaced or backed by auth/session identity.

## Recommended frontend app structure

Suggested initial screens/routes:

```text
/                         Home / landing
/lobbies/new              Create lobby
/lobbies/{lobbyId}        Lobby room
/games/{gameId}           Game table
```

Optional helper screens later:

```text
/join                     Join by lobby id/code
/spectate/{gameId}        Read-only game view
```

## Screen 1: Home / landing

Purpose:

- explain the game briefly,
- let a user create a lobby,
- let a user join an existing lobby by id/code.

Primary actions:

```text
Create lobby
Join lobby
```

Minimum state needed:

```text
none
```

## Screen 2: Create lobby

Purpose:

Host configures the lobby and game setup.

Fields:

```text
Lobby name
Host display name
Max players: 2..6
Public/private
Initial health: 1..50, default 10
Target VP/XP: 1..100, default 20
```

API call:

```http
POST /api/lobbies
Content-Type: application/json
```

Request:

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

Response shape:

```json
{
  "lobby": {
    "lobbyId": "guid",
    "name": "Friday kaiju night",
    "maxPlayers": 4,
    "isPublic": true,
    "initialHealth": 10,
    "targetVictoryPoints": 20,
    "status": "WaitingForPlayers",
    "gameId": null,
    "seats": [
      {
        "playerId": 0,
        "displayName": "Host",
        "isHost": true,
        "isReady": true,
        "playerToken": "guid"
      }
    ]
  },
  "playerToken": "guid",
  "playerId": 0
}
```

Frontend should store locally:

```text
lobbyId
playerToken
playerId
isHost
```

For the first prototype, `localStorage` is acceptable.

## Screen 3: Lobby room

Purpose:

Players wait before the game starts.

UI elements:

```text
Lobby name
Lobby id/copy invite button
Game setup summary
Seats/player list
Ready toggle for current player
Start game button for host
Status label: WaitingForPlayers / ReadyToStart / Started
```

### Get lobby snapshot

```http
GET /api/lobbies/{lobbyId}
```

Use polling for first prototype:

```text
interval: 1000ms to 2000ms
```

### Join lobby

```http
POST /api/lobbies/{lobbyId}/join
Content-Type: application/json
```

Request:

```json
{
  "displayName": "Guest"
}
```

Frontend stores returned:

```text
playerToken
playerId
```

### Set ready

```http
POST /api/lobbies/{lobbyId}/ready
Content-Type: application/json
```

Request:

```json
{
  "playerToken": "guid",
  "isReady": true
}
```

### Start lobby

Only host should show this button.

Enable when:

```text
lobby.status == ReadyToStart
current seat is host
```

API call:

```http
POST /api/lobbies/{lobbyId}/start
Content-Type: application/json
```

Request:

```json
{
  "playerToken": "guid"
}
```

Response shape:

```json
{
  "lobby": {
    "lobbyId": "guid",
    "status": "Started",
    "gameId": "guid"
  },
  "game": {
    "gameId": "guid",
    "status": "Setup",
    "players": []
  }
}
```

After success:

```text
navigate to /games/{game.gameId}
```

Non-host clients can discover start by polling lobby. When `lobby.gameId` is not null, navigate to `/games/{lobby.gameId}`.

## Screen 4: Game table

Purpose:

Main playable game UI.

High-level layout:

```text
Top: game status / current player / winner banner
Left: player panels
Center: Tokyo board + dice area
Right: market cards
Bottom: command/action panel
Side/bottom drawer: event log
```

### Get game snapshot

```http
GET /api/games/{gameId}
```

Use snapshot polling for first prototype:

```text
interval: 1000ms to 2000ms
```

Snapshot should drive rendering of:

```text
players
current player
turn state
Tokyo city/bay occupants
market
pending decision
winner
version
```

### Get incremental events

```http
GET /api/games/{gameId}/events?after={lastEventSequence}
```

Frontend keeps:

```text
lastEventSequence
```

On response:

```text
append returned events to event log
update lastEventSequence = currentEventSequence
optionally trigger simple animations/toasts
```

Recommended initial polling:

```text
events: every 750ms to 1500ms
snapshot: every 1500ms to 2500ms or after command success
```

## Game commands

The API currently exposes direct command endpoints. For the first UI, the frontend can enable/disable buttons based on:

```text
GameStateDto.Status
CurrentPlayerId
TurnStateDto
PendingDecision
local playerId
```

A future backend improvement can add `AvailableActionsDto`, but it is not required for the first prototype.

### Core flow commands

```http
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

Common actor request:

```json
{
  "actorPlayerId": 0
}
```

Reroll request:

```json
{
  "actorPlayerId": 0,
  "diceIndexesToReroll": [0, 2, 4]
}
```

Buy face-up card request:

```json
{
  "actorPlayerId": 0,
  "slotIndex": 1
}
```

Choose leave Tokyo request:

```json
{
  "actorPlayerId": 1,
  "leaveTokyo": true
}
```

### Special card command endpoints

```http
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

These can be wired incrementally in the UI after the core turn loop works.

## First playable UI milestone

The first milestone should support this full flow:

```text
Create lobby
Join lobby in another browser tab
Guest ready
Host start
Navigate both clients to game table
Initialize game
Begin turn
Roll dice
Finalize dice
Choose Tokyo leave decision if needed
End turn
Advance player
Read event feed
```

This is enough to validate the whole server/UI shape.

## Suggested frontend implementation order

1. Project setup and routing.
2. Typed API client wrappers.
3. Home + create lobby form.
4. Lobby room with polling.
5. Start lobby and navigate to game table.
6. Game table read-only snapshot rendering.
7. Core command buttons for turn loop.
8. Event feed polling.
9. Basic market rendering and buy/refresh commands.
10. Incremental special card command UI.

## UI state model

Minimum client state:

```ts
type ClientSession = {
  lobbyId?: string;
  gameId?: string;
  playerId?: number;
  playerToken?: string;
  lastEventSequence?: number;
};
```

The server remains the source of truth for game/lobby state.

## Known backend follow-ups after UI starts

These are not blockers for starting UI:

- add `AvailableActionsDto` if button-state logic gets too complex,
- replace `actorPlayerId` command requests with token-validated requests,
- add real HTTP integration tests,
- add OpenAPI/Swagger,
- add persistence/reconnect,
- add SignalR or SSE if polling becomes too limiting.
