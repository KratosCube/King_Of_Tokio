# KingOfTokyo.Web

First Blazor WebAssembly prototype for the King of Tokyo online UI.

## Current scope

This project is intentionally an early playable UI skeleton.

Implemented in the first slice:

- app shell and routing,
- Home page,
- Create lobby page,
- Lobby room page with polling,
- Game table page with snapshot polling,
- initial API client,
- temporary client session state,
- basic command buttons for the core turn loop.

## Run/build

From repository root:

```bash
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

To run the API:

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Api/KingOfTokyo.Api.csproj
```

To run the Blazor app during development:

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Web/KingOfTokyo.Web.csproj
```

The first prototype currently expects API calls under `/api/...`. Local development may need proxy/static hosting alignment depending on how API and Web are launched.

## Next UI steps

1. Verify build and basic launch.
2. Add API base URL configuration or dev proxy strategy.
3. Persist `ClientSessionState` to browser storage.
4. Add event feed polling to `GameTable`.
5. Improve command availability logic.
6. Add market buy/refresh controls.
7. Add pending decision UI.
8. Start polishing the board layout and dice interactions.
