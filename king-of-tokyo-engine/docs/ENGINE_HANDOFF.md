# King of Tokyo / Vládce Tokia project handoff

This is the current handoff for the project in its **headless engine + early Web UI** phase.

Repository and branch currently used in this work:

```text
https://github.com/KratosCube/King_Of_Tokio/tree/rollback-before-dev-player-control/king-of-tokyo-engine
```

Use only this project path:

```text
king-of-tokyo-engine
```

There have been accidental typo paths in the past such as `king-of-tokio-engine`. Do not add new files there. If such files appear in search results, treat them as stale unless they also exist under `king-of-tokyo-engine`.

Primary validation commands from repository root:

```bash
git pull
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current high-level status

The project is now a **functional pre-MVP prototype**:

- the headless engine is broad and mostly complete for the represented/default deck,
- the API/lobby layer exists,
- the Blazor Web UI exists and can play games,
- the current active work is **stabilizing special-card UI and testing all cards that require UI decisions/actions**.

Do not assume the full suite is green until the user confirms a fresh local run. The latest reported local build failed only because `MarketRefreshFlowTests.cs` missed a `GameOptions` import; this was fixed in commit `84f8ee065865b33e436c72da49724690b5005939`, but a fresh green run has not yet been reported.

## Work style to continue

- Answer in Czech.
- Keep changes small to medium.
- Prefer one safe logical step per commit.
- If production code is added, add/update tests where practical.
- Keep build and tests green. If something breaks, fix that first.
- Use existing command pattern, validators, services, domain state, DTOs, API endpoints, and events.
- After every change, report what changed, commit SHA, and the local command to run.
- Be careful with large files, especially `GameEngine.cs` and `GameTable.razor`: GitHub connector updates whole files, and full rewrites have caused accidental regressions before.
- The user is now okay with UI work; older docs saying “do not work on UI yet” are outdated for this branch.

## Files to read first

```text
docs/ENGINE_HANDOFF.md
docs/UI_CARD_ACTIONS_HANDOFF.md
docs/ENGINE_REMAINING_WORK_PLAN.md
docs/ENGINE_TODO.md
docs/CARD_IMPLEMENTATION_AUDIT.md
docs/MIMIC_POLICY_NOTES.md
docs/OWNED_CARD_LIFECYCLE_AUDIT.md
src/KingOfTokyo.Core/Engine/GameEngine.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
src/KingOfTokyo.Core/Domain/State/GameState.cs
src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
src/KingOfTokyo.Core/Services/SpecialCardActivationService.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
src/KingOfTokyo.Core/Services/MarketPurchaseService.cs
src/KingOfTokyo.Core/Rules/Attack/DamageApplier.cs
src/KingOfTokyo.Api/Endpoints/GameEndpoints.cs
src/KingOfTokyo.Api/Contracts/ApiDtos.cs
src/KingOfTokyo.Web/Pages/GameTable.razor
src/KingOfTokyo.Web/Components/PlayerKeepCards.razor
src/KingOfTokyo.Web/Services/ApiClient.cs
src/KingOfTokyo.Web/Contracts/ApiContracts.cs
tests/KingOfTokyo.Core.Tests
tests/KingOfTokyo.Api.Tests
```

## Current architecture snapshot

Current shape:

- `GameEngine` dispatches commands and coordinates services.
- `GameState` owns public game state, versioning, event log, current turn, Tokyo state, market, pending decisions, scheduled turns, and winner info.
- `GameStateDto` / `GameStateDtoMapper` provide snapshot projection.
- `GameEventCursorMapper` provides incremental event sync by event sequence.
- `KingOfTokyo.Api` exposes command endpoints and lobby/game sessions.
- `KingOfTokyo.Web` is a Blazor WebAssembly UI over the API.
- Keep-card action UI is moving toward “click Action badge under the keep card”, not a global bottom panel.
- Card art assets are expected under `/images/cards/{cardId}.jpg` in the Web app.

## Latest known commits / notable recent changes

Recent important commits in this branch include:

```text
0cb2ae235d9e76dbc7a5aa49086ab723ce41a64e  Stop mutating dice face DOM from JavaScript
4c4ba1bcdf2275e4cd3e82ad6390f81d6c02cd11  Restore lightweight dice action layout
895b79ffa481364b316895c40e7ab2457dfccaee  Add web request DTOs for MVP card actions
a1a2ffeff4d217806884216c497f876fb494002b  Expose MVP keep card action commands in web client
4dccd8eda4c05d25a6e30062f5341becd26e4185  Add MVP keep card actions to game table
708c8faba01bebd382335e51d4ab584621077ab5  Pass keep card owner to click handlers
7d3f8586ca99ea61a44ce6bb5afec5f92c4109a7  Drive MVP keep card actions from card clicks
216ad7d69dd07317efa1b8ee7d34e240e2272cb1  Use action badge for keep card actions
9cdf2342b2a7e6530d3cad9e17cd25bbf22b5108  Style keep card action badges as buttons
7d9515b7adfacbaa182e75dcbdab5eb3e77f330b  Award energy for stronger damage trigger
ff9cb39ca1003a2df889ca83b1f72d563cb4cdb0  Expose opportunist decision commands in web client
074b6dd1a06ed4af94b500bcb39c68772ef941aa  Expose opportunist decline command endpoint
84f8ee065865b33e436c72da49724690b5005939  Fix GameOptions import in market refresh tests
```

## Headless engine status

The represented/default deck is broadly implemented as headless logic. Important implemented systems include:

- turn lifecycle and automatic advance flow,
- Tokyo City / Tokyo Bay lifecycle and edge cases,
- 2-player games,
- dice roll / reroll / finalize,
- market, refresh, top-deck purchase, and owned keep-card transfer,
- event log + versioning,
- DTO snapshot mapping and event cursor mapping,
- victory and elimination timing,
- damage prevention/cancellation/replacement,
- Poison/Shrink tokens,
- keep-card lifecycle for current production paths,
- Mimic v1 policy,
- Omnivore,
- Healing Ray,
- Opportunist and Psychic Probe reaction windows,
- Background Dweller automatic reroll of `Three`,
- Monster Batteries stored energy/payment support,
- Freeze Time / Frenzy extra turn scheduling.

Important recent bug fix:

- **We're Only Making It Stronger** now grants energy, not victory points, when the owner loses 2+ HP. The internal method name is still misleading (`GetVictoryPointsWhenTakingDamage`), so it should be renamed and regression-tested later.

## Current Web UI status

The UI is playable, but special-card UI is incomplete and is the main active risk.

Implemented / partly implemented in Web UI:

- dice are rendered by Blazor; JS no longer mutates dice face DOM,
- keep cards show card art and action badges,
- clicking the card image is for detail/card art,
- clicking the `Action` badge is intended to drive card actions,
- Made in a Lab has API/client support and a partial pending-decision panel,
- Rapid Healing has API/client support and a basic keep-card action,
- Wings has API/client support and a basic keep-card action,
- Healing Ray has API/client support and basic target/amount UI gated by available heart dice,
- Mimic has API/client support and a basic target-selection UX using keep-card clicks,
- Opportunist API/client support has been partly added.

Known current UI blockers / follow-up:

1. **OpportunistPurchase UI is still missing in `GameTable.razor`.**
   - API endpoint exists for buy.
   - API endpoint for decline was added.
   - Web client methods exist for buy/decline.
   - The UI still needs to show `Buy revealed card` / `Skip` for `PendingDecision.DecisionType == "OpportunistPurchase"`.
   - This can currently make the game appear stuck after buying/refreshing a card that reveals a new card while an eligible Opportunist owner exists.
2. **Mimic action timing is too permissive in UI.**
   - Engine enforces timing, but UI should only offer Mimic retarget when legal:
     - initial target after buying Mimic in purchase phase if it has no target, or
     - at the start of its owner's turn before rolling, with enough energy, per card text/policy.
3. **Mimic copied active effects need UI verification.**
   - Engine supports copied effects for selected cards, but UI must check `card.MimicTarget` through `ActionCardProvides(...)` and offer the copied action only when that action is legal.
4. **Healing Ray UI must be verified against spent heart dice.**
   - It should use remaining unused hearts, not just total heart dice.
5. **Special card UI still needed for more cards:**
   - Telepath,
   - Stretchy,
   - Herd Culler,
   - Smoke Cloud,
   - Plot Twist,
   - Metamorph,
   - Psychic Probe,
   - Parasitic Tentacles,
   - Opportunist.

The detailed UI handoff/checklist is in:

```text
docs/UI_CARD_ACTIONS_HANDOFF.md
```

## Mimic v1 policy

Status: implemented for final v1 policy.

Supported copied effects include:

- passive keep effects through `KeepCardRulesService`,
- Acid Attack,
- Omnivore,
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray.

Intentionally blocked Mimic activations for v1:

- Smoke Cloud,
- Plot Twist,
- Metamorph,
- Psychic Probe.

Reason: these effects use card-local counters, once-per-turn state, self-discard, or special ownership/self semantics.

Policy docs/tests:

```text
docs/MIMIC_POLICY_NOTES.md
tests/KingOfTokyo.Core.Tests/Integration/MimicUnsupportedStatefulCardFlowTests.cs
```

## Recommended next step

1. Pull latest.
2. Run build/test.
3. If green, implement `OpportunistPurchase` UI in `GameTable.razor`.
4. Add/update tests for Opportunist API/UI-adjacent DTO behavior if feasible.
5. Tighten Mimic UI timing.
6. Continue through `docs/UI_CARD_ACTIONS_HANDOFF.md` until all UI-needing cards are covered and manually tested.

Commands:

```bash
git pull
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```
