# Engine TODO checklist

This checklist tracks the remaining headless game-logic work before starting the online UI/server layer.

Keep this file focused on engine behavior, rule policy, regression coverage, and sync-readiness helpers. UI work belongs elsewhere.

Before continuing a new block, run:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current verification

- [ ] Verify the full suite is green after the latest `It Has a Child` / Bay cleanup fixes, `Drop from High Altitude` Bay policy tests, victory timing edge-case tests, Vast Storm prevention/Eater tests, Evacuation Orders/Eater/Child tests, Gas Refinery prevention/Eater tests, Jets/prevention lethal timing tests, Mimic v1 policy documentation cleanup, owned-card lifecycle audit, and event cursor sync helper.

## Must finish before UI/server work

### Victory and elimination timing

- [x] Add regression coverage for multiple players reaching 20 VP in the same resolution step.
- [x] Add regression coverage where the current player and a non-current player both reach 20 VP through different events in the same turn.
- [x] Add regression coverage where all monsters are eliminated even though one or more players reached 20 VP earlier in the same turn.
- [x] Add regression coverage for `VictoryMode.LastMonsterStanding` when all monsters are eliminated: no winner.

### Remaining combined damage / prevention / Eater coverage

- [x] Add `Vast Storm` + `Eater of the Dead` + prevention/cancellation coverage.
- [x] Add `Evacuation Orders` + `Eater of the Dead` + `It Has a Child` coverage.
- [x] Add `Gas Refinery` + prevention + `Eater of the Dead` coverage.
- [x] Add lethal timing coverage around `Jets`, prevention, and Tokyo leave decisions.

### Mimic policy closure

- [x] Decide whether unsupported stateful/self-discard Mimic activations remain permanently blocked for v1.
- [x] If blocked for v1, document the final v1 policy in `ENGINE_HANDOFF.md` and `CARD_IMPLEMENTATION_AUDIT.md`.
- [x] If not blocked, implement explicit copy semantics for `Smoke Cloud`, `Plot Twist`, `Metamorph`, and `Psychic Probe`.

### Owned-card lifecycle cleanup

- [x] Review all direct `RemoveKeepCard(...)` paths and confirm each path handles lifecycle loss effects and Mimic cleanup.
- [x] Document current lifecycle state and remaining centralization refactor in `OWNED_CARD_LIFECYCLE_AUDIT.md`.

### DTO / sync readiness

- [x] Decide whether the server needs an event cursor / incremental sync helper on top of `GameState.EventLog` and `Version`.
- [x] If needed, add a headless helper and regression tests before the server layer.
- [x] Decide whether event DTOs need richer typed payloads for UI animations.

## Documentation cleanup

- [x] Update `CARD_IMPLEMENTATION_AUDIT.md` to reflect that `Omnivore` is implemented.
- [x] Update `CARD_IMPLEMENTATION_AUDIT.md` to reflect that `Healing Ray` generic dispatch is implemented.
- [x] Update `CARD_IMPLEMENTATION_AUDIT.md` with the recent Acid Attack, Fire Breathing, prevention, Wings, Jets, Eater, It Has a Child, and Drop from High Altitude coverage.
- [ ] Update `ENGINE_REMAINING_WORK_PLAN.md` after each completed checklist block.
- [ ] Update `ENGINE_HANDOFF.md` before stopping work or switching to UI/server tasks.

## Future cleanup / not blocking UI start

- [ ] Consider adding a central owned-card lifecycle entry point, for example `OnKeepCardLost`, `OnKeepCardDiscarded`, and `OnKeepCardTransferred`.
- [ ] Add dedicated unit tests for `KeepCardLifecycleService` if new lifecycle hooks are introduced.

## Recently completed / already covered

- [x] Bay / Tokyo core edge cases.
- [x] 2-player lifecycle support.
- [x] EventLog and Version regression coverage.
- [x] DTO baseline snapshot coverage.
- [x] Healing Ray command dispatch through `GameEngine.Execute(IGameCommand)`.
- [x] Keep-card lifecycle service for `Even Bigger` add/loss paths.
- [x] Mimic support for supported passive/activated copied effects.
- [x] Omnivore scoring and Mimic compatibility.
- [x] Acid Attack combined coverage for dice attack, Poison Quills, discard-card damage, self-damage exclusion, Fire Blast eliminations, Eater, Armor Plating, and Camouflage.
- [x] Tokyo attack modifier stacking for Burrowing, Urbavore, Spiked Tail, and Acid Attack.
- [x] Fire Breathing neighbor targeting across 2-6 players including Bay cases.
- [x] Camouflage with Fire Breathing.
- [x] Wings after bought-card damage and Poison Quills card-effect damage.
- [x] Jets with Tokyo leave damage, Burrowing, and Wings cancellation.
- [x] Eater of the Dead multi-elimination edge cases.
- [x] It Has a Child + Bay cleanup production fix and regression coverage.
- [x] Drop from High Altitude Bay policy coverage added.
- [x] Victory timing edge-case coverage for simultaneous 20 VP and all-eliminated scenarios.
