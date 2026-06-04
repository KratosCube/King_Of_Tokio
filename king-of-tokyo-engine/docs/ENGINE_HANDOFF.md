# King of Tokyo / Vládce Tokia engine handoff

This document is the current handoff for the headless King of Tokyo / Vládce Tokia engine before building the online server/UI layer.

Repository:

```text
https://github.com/KratosCube/King_Of_Tokio/tree/main/king-of-tokyo-engine
```

Use only this project path:

```text
king-of-tokyo-engine
```

There have been accidental typo paths in the past such as `king-of-tokio-engine`. Do not add new files there. If such files appear in search results, treat them as stale unless they also exist under `king-of-tokyo-engine`.

Primary validation command from repository root:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

If there is any doubt, start by running the full test suite locally.

## Work style to continue

- Keep changes small to medium.
- Prefer one safe logical step per commit.
- If production code is added, add or update tests in the same step.
- Keep the headless engine green before moving on.
- If something breaks, fix that first.
- Do not work on UI until the latest full test run is green.
- Prefer existing command pattern, services, validators, domain state, DTOs, and events.
- After every change, report what changed, commit SHA, and the local command to run.
- Be careful with `GameEngine.cs`: GitHub connector updates whole files, and full rewrites have caused accidental regressions before. Prefer service/test changes unless a `GameEngine` change is necessary.

## Files to read first

```text
docs/ENGINE_HANDOFF.md
docs/ENGINE_TODO.md
docs/ENGINE_REMAINING_WORK_PLAN.md
docs/CARD_IMPLEMENTATION_AUDIT.md
docs/MIMIC_POLICY_NOTES.md
docs/OMNIVORE_IMPLEMENTATION_NOTES.md
docs/OWNED_CARD_LIFECYCLE_AUDIT.md
src/KingOfTokyo.Core/Engine/GameEngine.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
src/KingOfTokyo.Core/Domain/State/GameState.cs
src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
src/KingOfTokyo.Core/Services/KeepCardLifecycleService.cs
src/KingOfTokyo.Core/Services/MimicService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
src/KingOfTokyo.Core/Services/MarketPurchaseService.cs
src/KingOfTokyo.Core/Services/OwnedCardTransferService.cs
src/KingOfTokyo.Core/Services/FinalizeDiceService.cs
src/KingOfTokyo.Core/Rules/Attack/DamageApplier.cs
src/KingOfTokyo.Core/Rules/Attack/DamagePreventionService.cs
src/KingOfTokyo.Core/Dto/GameStateDto.cs
src/KingOfTokyo.Core/Dto/GameStateDtoMapper.cs
src/KingOfTokyo.Core/Dto/GameEventCursorDto.cs
src/KingOfTokyo.Core/Dto/GameEventCursorMapper.cs
tests/KingOfTokyo.Core.Tests
```

## Current architecture snapshot

The project is a headless C# engine.

Current shape:

- `GameEngine` dispatches commands and coordinates services.
- `GameState` owns public game state, versioning, event log, current turn, Tokyo state, market, pending decisions, scheduled turns, and winner info.
- `GameStateDto` / `GameStateDtoMapper` provide full snapshot projection for future server/UI use.
- `GameEventCursorMapper` provides incremental event sync by event sequence.
- Most card logic is implemented through services, validators, domain helpers, command handlers, and integration tests.
- The codebase intentionally prioritizes stable headless rules before any UI.

## Latest known status

As of this handoff:

- Headless v1 gameplay logic is essentially complete pending the next confirmed green full-suite run.
- Most cards in the represented/default deck are implemented.
- No known card from the current represented/default deck is missing as headless logic.
- 2-player games are supported as a deliberate small rule change.
- EventLog + Versioning are implemented and covered.
- DTO snapshot mapping is stabilized and covered.
- Event-sequence cursor mapping exists for future incremental sync.
- Bay/Tokyo edge cases have broad regression coverage.
- Full-flow/API-readiness tests have broad regression coverage.
- Healing Ray is handled in the main `GameEngine.Execute(IGameCommand)` command switch.
- Keep-card lifecycle cleanup is safe for current production paths and documented.
- Mimic v1 policy is final: stateful/self-discard activations through Mimic are intentionally blocked.
- Combined card edge-case regression coverage has been expanded across the previously risky clusters.

Latest required validation:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current TODO status

The active checklist is:

```text
docs/ENGINE_TODO.md
```

Current blocking item before starting server/UI work:

- verify the full suite is green after the latest event cursor helper and documentation cleanup.

Already completed in the checklist:

- victory and elimination timing edge cases,
- combined damage / prevention / Eater coverage,
- Mimic v1 policy closure,
- owned-card lifecycle review,
- DTO / sync readiness helper,
- card audit cleanup for Omnivore, Healing Ray, Mimic, and recent coverage.

Remaining non-blocking future cleanup:

- optional central owned-card lifecycle entry point,
- additional `KeepCardLifecycleService` tests if new lifecycle hooks are introduced,
- strongly typed serialized event DTOs if UI animations need them,
- API adapter tests once the server layer exists.

## Recently completed stabilization work

### Bay / Tokyo edge cases

Covered:

- 5-player game uses Bay.
- 2-4 player games do not use Bay.
- Bay disables below 5 alive players.
- Bay occupant cleanup on elimination.
- City/Bay leave decisions and queued decisions.
- Bay-only occupant attack flows.
- Bay cleanup with `It Has a Child` revive.
- Drop from High Altitude Bay policy.
- 2-player lifecycle.

### Full-flow / API-readiness regression tests

Covered:

- full turn lifecycle,
- representative Bay attack/purchase flow,
- EventLog and Version increments,
- CommandResult success/failure/pending decision paths,
- DTO snapshots,
- full-game victory by 20 VP and last monster standing,
- no invalid victory for dead 20 VP players,
- event-sequence cursor mapping for incremental sync.

### Victory and elimination timing

Covered:

- current player with 20 VP must survive the turn,
- dead player with 20 VP cannot win,
- non-current alive player can win with 20 VP,
- multiple players reaching 20 VP in the same resolution step,
- current and non-current player reaching 20 VP in the same turn,
- all monsters eliminated even though someone reached 20 VP earlier,
- `VictoryMode.LastMonsterStanding` with all monsters eliminated results in no winner,
- Eater of the Dead timing with multiple eliminations and It Has a Child.

### Combined card edge-case coverage

Covered:

- Acid Attack + dice attack,
- Acid Attack + Poison Quills,
- Acid Attack + discard-card damage,
- Acid Attack self-damage exclusion,
- Acid Attack + Gas Refinery,
- Acid Attack + High Altitude Bombing,
- Acid Attack + Fire Blast eliminations + Eater,
- Acid Attack + Armor Plating,
- Acid Attack + Camouflage,
- Acid Attack + Armor Plating + Camouflage,
- Mimic -> Acid Attack with Fire Blast, dice attack, and Poison Quills,
- Tokyo attack modifiers: Burrowing, Urbavore, Spiked Tail, and Acid Attack stacking,
- Fire Breathing neighbor targeting in 2-6 player games, including Bay occupants,
- Camouflage + Fire Breathing,
- Wings after bought-card damage,
- Wings after Poison Quills card-effect damage,
- Jets + Tokyo leave damage + Burrowing + Wings,
- Vast Storm + prevention/cancellation + Eater,
- Evacuation Orders + Eater + It Has a Child,
- Gas Refinery + prevention + Eater,
- lethal timing around Jets, prevention, and Tokyo leave decisions.

## Important current systems/cards

### Mimic

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

Reason: these effects use card-local counters, once-per-turn state, self-discard, or special ownership/self semantics. This is a v1 policy decision, not an implementation gap.

Policy docs/tests:

```text
docs/MIMIC_POLICY_NOTES.md
tests/KingOfTokyo.Core.Tests/Integration/MimicUnsupportedStatefulCardFlowTests.cs
```

### Omnivore

Status: implemented.

Implemented pieces:

- `KnownCardIds.Omnivore` exists.
- Default deck entry exists.
- +2 VP when the owner rolled at least one pair.
- Dice used for Omnivore are not consumed.
- Multiple pairs still give one bonus per Omnivore effect.
- Mimic -> Omnivore works.
- Real Omnivore + Mimic -> Omnivore stacks.
- Omnivore works alongside Complete Destruction.

### Healing Ray

Status: implemented.

Implemented pieces:

- command exists and is handled through main `GameEngine.Execute(IGameCommand)` switch,
- typed extension remains compatible,
- target pays 2 energy per actual healed damage or all remaining energy if insufficient,
- spent heart dice are tracked on `TurnState`,
- Mimic -> Healing Ray works,
- stale Mimic target cleanup for copied Healing Ray is covered.

### Owned-card lifecycle

Status: safe for current v1 production paths.

Audit doc:

```text
docs/OWNED_CARD_LIFECYCLE_AUDIT.md
```

Current conclusion:

- no known v1 gameplay bug remains in owned-card removal/lifecycle handling,
- most production removal paths already call lifecycle lost effects and Mimic cleanup,
- `Psychic Probe` self-discard does not call `ApplyLostEffect`, but this is safe in v1 because Psychic Probe has no lost lifecycle effect and Mimic cleanup is already called,
- a central owned-card lifecycle helper is future cleanup, not a UI/server blocker.

### DTO / sync readiness

Status: implemented enough for starting server/API work.

Snapshot DTO:

```text
src/KingOfTokyo.Core/Dto/GameStateDto.cs
src/KingOfTokyo.Core/Dto/GameStateDtoMapper.cs
```

Event cursor DTO/helper:

```text
src/KingOfTokyo.Core/Dto/GameEventCursorDto.cs
src/KingOfTokyo.Core/Dto/GameEventCursorMapper.cs
tests/KingOfTokyo.Core.Tests/Dto/GameEventCursorMapperTests.cs
```

Important sync rule:

- `GameState.Version` increments for every successful command, including commands with no events.
- Event cursoring therefore uses event sequence / `EventLog.Count`, not `GameState.Version`.
- `CurrentGameVersion` is still included in the cursor DTO as snapshot metadata.

## Documentation map

Use these documents for current status:

```text
docs/ENGINE_TODO.md
docs/ENGINE_REMAINING_WORK_PLAN.md
docs/CARD_IMPLEMENTATION_AUDIT.md
docs/MIMIC_POLICY_NOTES.md
docs/OMNIVORE_IMPLEMENTATION_NOTES.md
docs/OWNED_CARD_LIFECYCLE_AUDIT.md
```

## Recommended next step

1. Run the full test suite.
2. If green, start designing the server/API layer over the headless engine.
3. If red, fix the failing compile/test issue first.

Command:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```
