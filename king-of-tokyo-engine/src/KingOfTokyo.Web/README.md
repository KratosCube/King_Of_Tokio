# KingOfTokyo.Web

First Blazor WebAssembly prototype for the King of Tokyo online UI.

## Current scope

This project is intentionally an early playable UI skeleton.

Implemented so far:

- app shell and routing,
- Home page,
- Create lobby page,
- colorful monster picker,
- Lobby room page with polling,
- colorful lobby monster cards,
- Game table page with snapshot polling,
- Tokyo board and dice visualization,
- initial API client,
- configurable API base URL,
- temporary client session state,
- basic command buttons for the core turn loop.

## Run/build

From repository root:

```bash
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

Run the API on port 5000:

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Api/KingOfTokyo.Api.csproj --urls http://localhost:5000
```

Run the Blazor app on port 5173:

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Web/KingOfTokyo.Web.csproj --urls http://localhost:5173
```

Open:

```text
http://localhost:5173
```

The Blazor app reads its API base URL from:

```text
wwwroot/appsettings.json
```

Default:

```json
{
  "ApiBaseUrl": "http://localhost:5000/"
}
```

The API currently allows CORS from local Blazor development origins on port `5173`.

## First manual smoke test

1. Start API on `http://localhost:5000`.
2. Start Web on `http://localhost:5173`.
3. Open Web.
4. Create a lobby.
5. Copy the lobby URL into another browser tab.
6. Join as another monster.
7. Mark guest ready.
8. Start game as host.
9. Use the basic command buttons on the game table.

## Next UI steps

1. Persist `ClientSessionState` to browser storage.
2. Add event feed polling to `GameTable`.
3. Improve command availability logic.
4. Add market buy/refresh controls.
5. Add pending decision UI.
6. Start polishing the board layout and dice interactions.
