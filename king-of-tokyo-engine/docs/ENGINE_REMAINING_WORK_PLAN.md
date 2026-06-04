# Remaining engine work plan

This document tracks the remaining headless-engine work before starting the online web UI/server layer.

The detailed checklist lives in:

```text
docs/ENGINE_TODO.md
```

Primary validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current status

The headless engine is now in final stabilization / handoff mode.

Most v1 gameplay logic is implemented, including:

- Bay / Tokyo lifecycle and edge cases,
- 2-player support,
- turn lifecycle,
- command success/failure/pending-decision handling,
- EventLog and Version tracking,
- DTO snapshot mapping,
- event-sequence cursor mapping for incremental sync,
- core victory and elimination timing,
- keep-card lifecycle behavior for current production paths,
- Mimic v1 policy,
- Omnivore,
- Healing Ray,
- combined-card regression coverage around Acid Attack, Fire Breathing, Wings, Jets, Eater, It Has a Child, Drop from High Altitude, Vast Storm, Evacuation Orders, and Gas Refinery.

## Must do before switching to UI/server work

1. Pull latest changes.
2. Run the full suite.
3. Fix any red tests before doing anything else.
4. Update `ENGINE_HANDOFF.md` after a confirmed green run.

Command:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Recently completed blocks

### Combined damage / prevention / Eater coverage

Completed:

- Acid Attack + Armor Plating,
- Acid Attack + Camouflage,
- Acid Attack + Armor Plating + Camouflage,
- Acid Attack + Fire Blast eliminations + Eater of the Dead,
- Tokyo attack modifiers: Burrowing, Urbavore, Spiked Tail, Acid Attack stacking,
- Fire Breathing neighbor targeting in 2-6 player games including Bay cases,
- Camouflage + Fire Breathing,
- Wings after bought-card damage and Poison Quills card-effect damage,
- Jets + Tokyo leave damage + Burrowing + Wings,
- Eater multi-elimination edge cases,
- It Has a Child + Bay cleanup,
- Drop from High Altitude Bay policy,
- Vast Storm + prevention/cancellation + Eater,
- Evacuation Orders + Eater + It Has a Child,
- Gas Refinery + prevention + Eater,
- lethal timing around Jets, prevention, and Tokyo leave decisions.

### Victory and elimination timing

Completed:

- current player must survive to win with 20 VP,
- dead player with 20 VP cannot win,
- non-current alive player can win with 20 VP,
- multiple players reaching 20 VP in the same resolution step,
- current and non-current player reaching 20 VP in the same turn,
- all monsters eliminated even though someone reached 20 VP earlier,
- `VictoryMode.LastMonsterStanding` with all monsters eliminated results in no winner.

### Mimic v1 policy closure

Completed:

- Mimic v1 policy is final for current engine scope.
- Supported copied effects are implemented and tested.
- Stateful/self-discard activations remain intentionally blocked through Mimic for v1:
  - Smoke Cloud,
  - Plot Twist,
  - Metamorph,
  - Psychic Probe.
- This is documented in `MIMIC_POLICY_NOTES.md` and `CARD_IMPLEMENTATION_AUDIT.md`.

### Owned-card lifecycle review

Completed:

- Production `RemoveKeepCard(...)` paths were reviewed.
- No current v1 gameplay blocker was found.
- Findings are documented in:

```text
docs/OWNED_CARD_LIFECYCLE_AUDIT.md
```

The remaining central lifecycle pipeline is future cleanup, not a UI/server blocker.

### DTO / sync readiness

Completed:

- Existing `GameStateDto` snapshot mapper remains stable.
- Added an event-sequence cursor helper:

```text
src/KingOfTokyo.Core/Dto/GameEventCursorDto.cs
src/KingOfTokyo.Core/Dto/GameEventCursorMapper.cs
tests/KingOfTokyo.Core.Tests/Dto/GameEventCursorMapperTests.cs
```

Important sync note:

- `GameState.Version` increments for every successful command, even if the command emits no events.
- Event cursoring therefore uses `EventLog.Count` / event sequence, not game version.
- Cursor DTO still includes `CurrentGameVersion` as snapshot metadata.

## Future cleanup / not blocking UI start

These are useful but not required before starting the server/UI layer:

- Central owned-card lifecycle helper, such as `OnKeepCardLost`, `OnKeepCardDiscarded`, and `OnKeepCardTransferred`.
- More unit tests for `KeepCardLifecycleService` if more lifecycle hooks are introduced.
- Refactor large card-effect branches into smaller effect handlers.
- Strongly typed serialized event DTOs if UI animations need them.
- API adapter tests once the server layer exists.
- A direct invariant test that a monster cannot occupy both City and Bay, if not already covered indirectly.
- Event log bounding/snapshot strategy for persistence.

## Recommended next step

Run the full suite.

If green, update `ENGINE_HANDOFF.md` and start the server/API layer.

If red, fix the failing test or compile error first.
