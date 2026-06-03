# Remaining engine work plan

This document tracks the remaining headless-engine work before starting the online web UI.

The engine is already in the late stabilization phase. Most major systems and cards are implemented. The remaining work is mostly edge-case regression coverage, small rule-policy fixes, cleanup/refactor work, and online-sync preparation.

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

## 1. Bay / Tokyo edge cases

Priority: high.

Reason: Tokyo and Bay state will be visible and interactive in the online UI, so edge cases need to be locked before frontend work.

Planned coverage:

- 5-player game uses Bay.
- 2-4 player games do not use Bay.
- When alive player count falls below 5, Bay is no longer usable.
- If Bay is occupied when alive player count falls below 5, Bay occupant must leave Tokyo and Bay must be cleared.
- Outside attacker targets both City and Bay occupants.
- Tokyo leave decisions work correctly for City and Bay occupants.
- If both City and Bay are empty in a Bay game, outside attack does not damage anyone and attacker enters City first.
- If City is occupied and Bay is empty in a Bay game, outside attacker can enter Bay after attack when rules require it.
- A monster cannot occupy both City and Bay.
- Drop from High Altitude behavior in Bay games needs explicit policy coverage.

## 2. Combined card edge cases

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

## 3. Victory and elimination timing

Priority: medium-high.

Already expanded:

- Current player with 20 VP must survive the turn.
- If current player reaches 20 VP but dies, they do not win.
- If no monsters remain alive, game ends with no winner.
- Non-current alive player can win with 20 VP.
- Dead player with 20 VP cannot win.
- VictoryMode.Standard, FirstToTwentyPoints, and LastMonsterStanding have targeted resolver tests.
- Eater of the Dead can trigger a non-current 20 VP win.

Still useful to cover:

- Multiple players reach 20 VP in same resolution step.
- Current player and non-current player reach 20 VP through different events in same turn.
- All monsters eliminated while one or more players reached 20 VP earlier in same turn.
- LastMonsterStanding mode with all eliminated should still produce no winner.

## 4. Healing Ray command cleanup

Priority: medium.

Current state:

- Healing Ray works through `GameEngineHealingRayExtensions` typed overload.
- This is functional but not uniform with the main `IGameCommand` switch.

Planned work:

- Move `ActivateHealingRayCommand` handling into main `GameEngine.Execute(...)` switch.
- Keep typed extension temporarily or remove if redundant.
- Add regression tests ensuring generic `IGameCommand` dispatch works.
- Verify Mimic -> Healing Ray still works.

## 5. Generic owned-card lifecycle hook

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

## 6. DTO / online sync readiness

Priority: medium before frontend.

Planned checks:

- DTO includes all state needed by a client:
  - game status,
  - winner info,
  - players,
  - health/max health,
  - energy,
  - VP,
  - keep cards,
  - counters,
  - stored energy,
  - Mimic target,
  - status tokens,
  - Tokyo/Bay occupants,
  - current turn,
  - dice pool,
  - pending decisions,
  - scheduled turns,
  - event log/version.
- EventLog supports client sync from version/cursor.
- Command result shape is enough for online API responses.
- Important UI animation events exist:
  - dice rolled/finalized,
  - damage dealt,
  - healing,
  - energy gained/spent,
  - VP gained,
  - card bought/discarded,
  - Tokyo enter/leave,
  - player eliminated,
  - game ended.

## 7. Documentation and audit cleanup

Priority: medium-low, but useful for future handoff.

Documents to keep updated:

- `docs/ENGINE_HANDOFF.md`
- `docs/CARD_IMPLEMENTATION_AUDIT.md`
- `docs/MIMIC_POLICY_NOTES.md`
- `docs/OMNIVORE_IMPLEMENTATION_NOTES.md`
- this file

Recent changes that should be reflected in handoff/audit:

- 2-player game support.
- VictoryResolver now handles non-current alive 20 VP winners.
- Eater of the Dead victory timing coverage.
- It Has a Child + Eater victory timing coverage.
- Acid Attack now applies to selected non-attack damage sources.
- Mimic -> Acid Attack discard damage regression coverage.

## Suggested next step

Start with Bay/Tokyo edge cases.

First small test target:

```text
In a 5-player game, when the alive player count drops below 5 and Bay is occupied, the Bay occupant leaves Tokyo and Bay is cleared.
```

Then continue with:

```text
Outside attack behavior against City + Bay occupants.
```
