# Unified lobby architecture

This document describes how the current King of Tokyo lobby can evolve into a reusable lobby system for multiple future games.

## Goal

Build one shared lobby layer that can support multiple online board/card games, while keeping each game's rules, setup fields, cosmetics, and start logic isolated behind a game-specific adapter.

The current King of Tokyo lobby is a good first implementation, but it should not become hard-coded forever as `KingOfTokyoOnlyLobby`.

## High-level direction

Split lobby concepts into two layers:

```text
Generic lobby layer
  -> game-agnostic room/player/ready/start state

Game-specific layer
  -> King of Tokyo setup options, monsters, avatars, and start adapter
```

## Generic lobby concepts

These should be reusable for any future game:

```text
LobbyId
GameType
Lobby name
Max players
Public/private visibility
Lobby status
Seats
Host seat
Player display name
Player token
Ready state
Created/started timestamps later
Game session id after start
```

Possible future generic DTO shape:

```csharp
public sealed record LobbyDto(
    Guid LobbyId,
    string GameType,
    string Name,
    int MaxPlayers,
    bool IsPublic,
    LobbyStatus Status,
    Guid? GameSessionId,
    IReadOnlyList<LobbySeatDto> Seats,
    object GameSetup,
    object GameCosmetics);
```

Possible future generic seat shape:

```csharp
public sealed record LobbySeatDto(
    int SeatIndex,
    string DisplayName,
    bool IsHost,
    bool IsReady,
    Guid PlayerToken,
    object GameSeatOptions);
```

The generic lobby does not need to understand what a monster, faction, deck, color, civilization, or hero means.

## Game-specific concepts for King of Tokyo

These should be moved toward a King of Tokyo-specific setup object over time:

```text
InitialHealth
TargetVictoryPoints
Selected monster id
Selected monster name
Selected avatar id
Tokyo monster catalog
King of Tokyo game start request mapping
```

Possible future DTOs:

```csharp
public sealed record KingOfTokyoLobbySetup(
    int InitialHealth,
    int TargetVictoryPoints);

public sealed record KingOfTokyoSeatOptions(
    string MonsterId,
    string MonsterName,
    string AvatarId);
```

## Game type registry

A future server registry can map a game type to its behavior:

```csharp
public interface ILobbyGameDefinition
{
    string GameType { get; }
    int MinPlayers { get; }
    int MaxPlayers { get; }
    object CreateDefaultSetup();
    object CreateDefaultSeatOptions(int seatIndex);
    object NormalizeSetup(object setup);
    object NormalizeSeatOptions(object seatOptions);
    GameStartDescriptor CreateGameStartDescriptor(LobbyDto lobby);
}
```

King of Tokyo implementation:

```csharp
public sealed class KingOfTokyoLobbyGameDefinition : ILobbyGameDefinition
{
    public string GameType => "king-of-tokyo";
}
```

Future examples:

```text
"king-of-tokyo"
"space-dice-arena"
"custom-card-battler"
"drafting-game"
```

## Start flow with abstraction

Future start flow:

```text
POST /api/lobbies/{lobbyId}/start
  -> generic lobby validates host + ready state
  -> resolve ILobbyGameDefinition by lobby.GameType
  -> game definition converts generic lobby into GameStartDescriptor
  -> game-specific session store creates the actual game
  -> lobby stores GameSessionId
```

For King of Tokyo, the adapter would convert:

```text
Lobby seats + KingOfTokyoSeatOptions
  -> CreateGameRequest(monster names, initial health, target VP)
```

## API shape options

### Option A: Generic endpoint with game type

```http
POST /api/lobbies
```

Request:

```json
{
  "gameType": "king-of-tokyo",
  "name": "Friday kaiju night",
  "maxPlayers": 4,
  "isPublic": true,
  "hostDisplayName": "Host",
  "gameSetup": {
    "initialHealth": 10,
    "targetVictoryPoints": 20
  },
  "hostSeatOptions": {
    "monsterId": "gigasaur",
    "monsterName": "Gigasaur",
    "avatarId": "avatar-roar"
  }
}
```

This is the best long-term shape.

### Option B: Keep current King of Tokyo endpoints temporarily

Current endpoints can stay while prototyping:

```text
POST /api/lobbies
GET  /api/lobbies/{lobbyId}
POST /api/lobbies/{lobbyId}/join
POST /api/lobbies/{lobbyId}/ready
POST /api/lobbies/{lobbyId}/start
```

The DTOs are currently still King-of-Tokyo flavored. That is acceptable for the first UI prototype, but they should be migrated before adding a second game.

## Recommended migration path

Do not do a large rewrite immediately. Use this staged path:

### Step 1: Keep current UI moving

Continue the King of Tokyo UI prototype with the existing lobby endpoints.

Reason:

```text
We need to validate the first playable flow before abstracting too much.
```

### Step 2: Introduce game type field

Add:

```text
GameType = "king-of-tokyo"
```

to lobby DTO/state.

This is a low-risk compatibility step.

### Step 3: Move King of Tokyo setup into nested object

Instead of top-level lobby fields:

```text
InitialHealth
TargetVictoryPoints
```

use:

```json
"gameSetup": {
  "initialHealth": 10,
  "targetVictoryPoints": 20
}
```

### Step 4: Move monster/avatar selection into seat options

Instead of top-level seat fields:

```text
MonsterId
MonsterName
AvatarId
```

use:

```json
"gameSeatOptions": {
  "monsterId": "gigasaur",
  "monsterName": "Gigasaur",
  "avatarId": "avatar-roar"
}
```

### Step 5: Add `ILobbyGameDefinition`

Extract King of Tokyo start conversion into a game definition/adapter.

### Step 6: Only then add another game

Once the second game is real, validate the abstraction with actual needs instead of guessing too early.

## UI impact

The UI can become a generic shell with game-specific panels:

```text
Generic lobby shell
  -> header/name/invite/status
  -> generic seat list
  -> ready/start controls
  -> game-specific setup component
  -> game-specific seat customization component
```

For King of Tokyo:

```text
KingOfTokyoLobbySetupPanel
KingOfTokyoMonsterPicker
KingOfTokyoSeatCard
```

Future games would provide their own:

```text
SomeOtherGameSetupPanel
SomeOtherGameSeatPicker
SomeOtherGameSeatCard
```

## Current recommendation

For the immediate next implementation work:

1. Do not stop UI progress.
2. Keep the colorful King of Tokyo lobby work.
3. Add `GameType = "king-of-tokyo"` soon as the first abstraction seed.
4. After the first playable UI flow works, refactor DTOs into generic lobby + King of Tokyo nested setup.

This avoids premature abstraction while keeping the code ready for a shared multi-game lobby later.
