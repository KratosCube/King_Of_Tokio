# Remaining work plan

This document tracks the current remaining work for the branch:

```text
rollback-before-dev-player-control
```

Current project path:

```text
king-of-tokyo-engine
```

Primary validation command from repository root:

```bash
git pull
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current status

The project is now past the pure-headless-only phase. The headless engine is broad and mostly implemented for the represented/default deck. The current active work is **special-card UI stabilization** in Blazor/Web plus regression tests for every card that requires UI decisions/actions.

Important companion docs:

```text
docs/ENGINE_HANDOFF.md
docs/UI_CARD_ACTIONS_HANDOFF.md
docs/CARD_IMPLEMENTATION_AUDIT.md
docs/ENGINE_TODO.md
```

## Must do now

1. Pull latest changes.
2. Run build and full test suite.
3. Fix any red build/test issue first.
4. Finish the blocking pending-decision UI gaps.
5. Add/verify tests for all special-card API/pending-decision flows where possible.
6. Manually browser-test the cards that require UI.
7. Keep this file and `ENGINE_HANDOFF.md` updated after meaningful milestones.

## Current top blockers

### 1. OpportunistPurchase UI

Status: backend/client mostly prepared, UI still missing.

Known problem:

- after buying/refreshing a market card, a new card may be revealed,
- if an eligible player has Opportunist, engine sets `PendingDecision.DecisionType == "OpportunistPurchase"`,
- `GameTable.razor` currently needs buttons to resolve that pending decision.

Already done:

- `BuyOpportunistRevealedCardCommand` exists,
- `DeclineOpportunistRevealedCardCommand` exists,
- API endpoint for `buy-opportunist-revealed-card` exists,
- API endpoint for `decline-opportunist-revealed-card` was added,
- `ApiClient` exposes buy/decline methods.

Still needed:

- `GameTable.razor` pending-decision panel for `OpportunistPurchase`, with `Buy revealed card` and `Skip`.

### 2. Mimic UI timing

Status: engine validates timing, UI is too permissive.

Fix UI to offer Mimic target selection only when legal:

- initial target when the real Mimic has no target and target selection is legal,
- retarget at the start of the Mimic owner's turn before rolling and after paying required energy,
- copied action display only when the copied action itself is legal.

### 3. Healing Ray UI remaining hearts

Status: basic UI exists, but verify it uses remaining unused heart dice.

If DTO does not expose `HealingRayHeartsSpent`, either add it or make UI conservative.

## UI card batches to finish

See `docs/UI_CARD_ACTIONS_HANDOFF.md` for detailed per-card status.

Recommended order:

1. Opportunist pending decision UI.
2. Mimic timing cleanup.
3. Manual smoke test of current MVP cards:
   - Made in a Lab,
   - Rapid Healing,
   - Wings,
   - Healing Ray,
   - Mimic,
   - Opportunist.
4. Telepath + Smoke Cloud UI.
5. Shared die-change UI for Herd Culler, Stretchy, Plot Twist.
6. Metamorph and Parasitic Tentacles keep-card selection UI.
7. Psychic Probe out-of-turn UI.
8. Browser/manual checklist for all UI-relevant cards.

## Recently completed headless-engine blocks

The following headless work is considered implemented or stabilized, though a fresh full-suite result should always be requested after new changes:

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
- Opportunist and Psychic Probe engine reaction windows,
- Background Dweller automatic reroll,
- Monster Batteries stored energy/payment support,
- Freeze Time / Frenzy scheduled turns,
- combined-card regression coverage around Acid Attack, Fire Breathing, Wings, Jets, Eater, It Has a Child, Drop from High Altitude, Vast Storm, Evacuation Orders, and Gas Refinery.

Recent engine correction:

- We're Only Making It Stronger should grant energy on losing 2+ HP. This was fixed in `DamageApplier`, but still needs a regression test and method rename cleanup.

## Future cleanup / not blocking current UI stabilization

- Rename `KeepCardRulesService.GetVictoryPointsWhenTakingDamage(...)` to match its current meaning for We're Only Making It Stronger energy.
- Add explicit regression tests for We're Only Making It Stronger energy gain, including Mimic copy.
- Central owned-card lifecycle helper, such as `OnKeepCardLost`, `OnKeepCardDiscarded`, and `OnKeepCardTransferred`.
- More unit tests for `KeepCardLifecycleService` if more lifecycle hooks are introduced.
- Refactor large card-effect branches into smaller effect handlers.
- Strongly typed serialized event DTOs if UI animations need them.
- API adapter tests for pending decisions and lobby/game sessions.
- Direct invariant test that a monster cannot occupy both City and Bay, if not already covered indirectly.
- Event log bounding/snapshot strategy for persistence.

## Recommended next step

Run the suite. If green, implement `OpportunistPurchase` UI in `GameTable.razor` next.
