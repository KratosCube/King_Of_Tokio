# Remaining engine work plan

This document tracks the remaining headless-engine work before starting the online web UI.

The engine is in the late stabilization phase. Most major systems and cards are implemented. The remaining work is mostly card-interaction regression coverage, small rule-policy fixes, cleanup/refactor work, and online-sync preparation.

Run this before continuing any new block:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current priorities

1. Keep build green.
2. Prefer small or medium changes.
3. Add regression tests for every production change.
4. Avoid large `GameEngine.cs` rewrites unless necessary.
5. Keep work focused on headless logic; no UI yet.

## Recently completed stabilization work

### Bay / Tokyo edge cases

Status: mostly complete for current engine policy.

Covered:

- 5-player game uses Bay.
- 2-4 player games do not use Bay.
- When alive player count falls below 5, Bay is disabled.
- If Bay is occupied when alive player count falls below 5, Bay occupant leaves Tokyo and Bay is cleared.
- Outside attacker targets both City and Bay occupants.
- Tokyo leave decisions work correctly for City and Bay occupants.
- City and Bay leave decisions are queued and resolved sequentially.
- City leaves / Bay leaves.
- City stays / Bay leaves.
- City stays / Bay stays.
- City leaves / Bay stays.
- Empty Tokyo in a Bay game enters City first.
- City occupied + Bay empty: if City defender stays, attacker enters Bay.
- City occupied + Bay empty: if City defender leaves, attacker enters City.
- Bay-only occupant can be attacked; if Bay occupant leaves, attacker enters City first.
- Bay occupant elimination clears Bay.
- Bay occupant elimination that drops alive count below 5 disables Bay.
- After Bay is disabled, later City leave decisions still send attacker to City, not Bay.

Still useful later:

- Drop from High Altitude behavior in Bay games needs explicit policy coverage.
- A direct invariant/unit test could verify a monster cannot occupy both City and Bay if not already enforced elsewhere.

### Full-flow / API-readiness regression tests

Status: mostly complete for current headless engine shape.

Covered:

- 5-player Bay attack -> queued leave decisions -> City entry -> Bay occupant stays -> purchase.
- 5-player Bay occupant elimination -> Bay cleanup/disable -> City decision -> purchase.
- 2-player lifecycle: initialize -> begin turn -> roll -> finalize -> enter Tokyo -> end turn -> advance -> next player begins.
- 2-player combat where Tokyo defender leaves and attacker enters City.
- 2-player combat where Tokyo defender stays and attacker remains outside.
- 2-player combat where defender is eliminated and game ends by last monster standing.
- EventLog and Version increment across multi-command combat/purchase flow.
- DTO snapshot after combat pending decision and after purchase state.
- CommandResult success, pending-decision, and failure paths.
- Turn lifecycle start-in-Tokyo VP scoring.
- Failed command state safety for wrong phase, pending decisions, and wrong actor.
- Full-game victory through 20 VP.
- Full-game victory through last monster standing.
- No invalid victory when a current player has 20 VP but is eliminated before surviving the turn.

Still useful later:

- API adapter tests once a real server layer exists.
- Event cursor / incremental sync tests if a cursor API is added on top of EventLog.

## 1. Combined card edge cases

Priority: high.

Reason: most cards work in isolation; remaining risk is card interaction.

Planned coverage:

### Acid Attack

Already expanded:

- Acid Attack adds damage to normal dice attack.
- Acid Attack adds damage to Poison Quills card-effect damage.
- Acid Attack adds damage to bought discard-card damage against other monsters.
- Acid Attack does not add to self-damage.
- Mimic -> Acid Attack works for bought discard-card damage.

Still useful to cover:

- Mimic -> Acid Attack with dice attack damage.
- Mimic -> Acid Attack with Poison Quills damage.
- Acid Attack with Gas Refinery.
- Acid Attack with High Altitude Bombing.
- Acid Attack with Fire Blast eliminations and Eater of the Dead.
- Acid Attack interaction with Armor Plating and Camouflage prevention.

### Eater of the Dead

Already expanded:

- Eater can award VP to a non-current player.
- A non-current alive player can win at 20 VP after Eater points.
- It Has a Child still counts as an elimination for Eater.

Still useful to cover:

- Multiple monsters eliminated at once gives Eater +3 per eliminated monster.
- Multiple Eater owners each gain points from the same elimination.
- Dead Eater owner gains no points.
- Eater points during all-monsters-eliminated scenarios do not create an invalid winner.

### It Has a Child

Already covered in important flows:

- Revives eliminated monster.
- Discards all cards.
- Clears energy.
- Counts as eliminated for Eater of the Dead.
- Mimic stale target cleanup is covered when owner is defeated and revived.

Still useful to cover:

- It Has a Child with Even Bigger max-health loss.
- It Has a Child when owner is in Bay and Bay cleanup is needed.
- It Has a Child plus multiple simultaneous eliminations.

### Even Bigger

Important remaining area:

- Losing Even Bigger should reduce max health by 2.
- If current health exceeds new max after losing Even Bigger, current health should clamp appropriately if that is current policy.
- Interactions with It Has a Child, Metamorph, Parasitic Tentacles, and elimination cleanup need coverage.

### Tokyo attack modifiers

Planned coverage:

- Burrowing when attacking into Tokyo.
- Burrowing damage when leaving Tokyo and new monster takes over.
- Urbavore extra damage from Tokyo.
- Spiked Tail + Acid Attack stacking.
- Fire Breathing neighbor targeting in 2, 3, 4, 5, and 6 player games.
- Fire Breathing with Bay occupants.

### Defensive / prevention cards

Planned coverage:

- Armor Plating with Acid Attack.
- Camouflage with Acid Attack.
- Camouflage with Fire Breathing.
- Wings after bought-card damage.
- Wings after Poison Quills or other card-effect damage, if allowed by policy.
- Jets with Tokyo leave damage and Burrowing.

## 2. Victory and elimination timing

Priority: medium-high.

Already expanded:

- Current player with 20 VP must survive the turn.
- If current player reaches 20 VP but dies, they do not win.
- If no monsters remain alive, game ends with no winner.
- Non-current alive player can win with 20 VP.
- Dead player with 20 VP cannot win.
- VictoryMode.Standard, FirstToTwentyPoints, and LastMonsterStanding have targeted resolver tests.
- Eater of the Dead can trigger a non-current 20 VP win.
- Full-game regression covers 20 VP victory, last-monster-standing victory, and dead 20 VP non-victory.

Still useful to cover:

- Multiple players reach 20 VP in same resolution step.
- Current player and non-current player reach 20 VP through different events in same turn.
- All monsters eliminated while one or more players reached 20 VP earlier in same turn.
- LastMonsterStanding mode with all eliminated should still produce no winner.

## 3. Healing Ray command cleanup

Priority: high before online API.

Current state:

- Healing Ray works through `GameEngineHealingRayExtensions` typed overload.
- This is functional but not uniform with the main `IGameCommand` switch.

Planned work:

- Move `ActivateHealingRayCommand` handling into main `GameEngine.Execute(...)` switch.
- Keep typed extension temporarily or remove if redundant.
- Add regression tests ensuring generic `IGameCommand` dispatch works.
- Verify Mimic -> Healing Ray still works.

## 4. Generic owned-card lifecycle hook

Priority: medium, but bigger refactor.

Current state:

- Mimic stale target cleanup works, but cleanup calls are scattered across services.

Goal:

Create central lifecycle hooks such as:

- `OnKeepCardAdded`
- `OnKeepCardLost`
- `OnKeepCardDiscarded`
- `OnKeepCardTransferred`

Use cases:

- Mimic stale target cleanup.
- Even Bigger max-health loss.
- Metamorph discard.
- Parasitic Tentacles transfer.
- Monster Batteries discard.
- Smoke Cloud, Plot Twist, Psychic Probe self-discard.
- It Has a Child discarding all keep cards.

Approach:

- Do not do this as one large rewrite.
- First add a small service method and test it.
- Then migrate one discard/transfer path at a time.

## 5. DTO / online sync readiness

Priority: medium before frontend.

Current state:

- DTO snapshot coverage exists for core game status, players, Tokyo, current turn, dice, pending decisions, market changes, and purchase flags.
- CommandResult regression coverage exists for success, pending decisions, and failures.
- EventLog/Version regression coverage exists across multi-command combat/purchase flow.

Still useful later:

- Add event cursor / incremental sync helper if needed by the server.
- Add API adapter tests once the online layer exists.
- Consider exposing a richer event DTO if UI animations need strongly typed serialized events.

## 6. Documentation and audit cleanup

Priority: medium-low, but useful for future handoff.

Documents to keep updated:

- `docs/ENGINE_HANDOFF.md`
- `docs/CARD_IMPLEMENTATION_AUDIT.md`
- `docs/MIMIC_POLICY_NOTES.md`
- `docs/OMNIVORE_IMPLEMENTATION_NOTES.md`
- this file

Recent changes that should be reflected in handoff/audit:

- 2-player game support.
- Expanded Bay/Tokyo regression coverage.
- Expanded full-flow/API-readiness regression coverage.
- VictoryResolver now handles non-current alive 20 VP winners.
- Eater of the Dead victory timing coverage.
- It Has a Child + Eater victory timing coverage.
- Acid Attack now applies to selected non-attack damage sources.
- Mimic -> Acid Attack discard damage regression coverage.

## Suggested next step

Recommended next block:

```text
Healing Ray command cleanup: move ActivateHealingRayCommand into the main GameEngine.Execute(...) switch and add generic-command regression tests.
```

After that, continue with either:

```text
Even Bigger / owned-card lifecycle cleanup
```

or:

```text
Combined card edge-case regression coverage
```
