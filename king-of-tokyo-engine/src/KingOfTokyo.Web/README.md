# King of Tokyo web UI

## Local play test

Install the .NET 10 SDK, then open two terminals at the repository root:

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Api/KingOfTokyo.Api.csproj --urls http://localhost:5000
```

```bash
dotnet run --project king-of-tokyo-engine/src/KingOfTokyo.Web/KingOfTokyo.Web.csproj --urls http://localhost:5173
```

Open `http://localhost:5173` in your usual browser profile. Create a lobby and copy the invite link shown on its page. Open that link in a **different browser profile or private window**. Separate tabs in the same profile share the same local player identity and cannot represent two players.

1. Join the lobby in the second profile and select **Set ready**.
2. Start the match in the host's profile and open the game in the guest's profile.
3. The host selects **Initialize game**. The starting player is chosen by attack rolls, so either profile may get the first turn.
4. In the starting player's profile, choose **Begin turn**, **Roll dice**, select dice to reroll if wanted, then **Finalize dice**.
5. Answer any Tokyo or card decisions in the indicated player's profile. Buy a card if affordable, then select **End turn**.
6. The player who just finished selects **Advance player**. The next player's **Begin turn** button then appears.
7. Check the event feed, energy, HP, market, and Tokyo occupancy after each action. The UI polls every two seconds; **Refresh** is available if needed.

The API stores lobbies and games in memory. Restarting it ends current matches. The default web configuration calls `http://localhost:5000/`, so this setup is intended for two profiles on the same computer.

## Automated checks

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

GitHub Actions also runs a browser smoke test for lobby creation, two independent players, the first turn, and handing the turn to the next player.
