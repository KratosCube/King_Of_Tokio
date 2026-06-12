# King of Tokyo engine - card implementation audit

This document tracks current card logic and UI-readiness status for the branch:

```text
rollback-before-dev-player-control
```

Validation command from repository root:

```bash
git pull
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

Detailed UI checklist:

```text
docs/UI_CARD_ACTIONS_HANDOFF.md
```

## Status legend

| Status | Meaning |
| --- | --- |
| Implemented | Present in the default deck and core effect is supported by engine code/tests. |
| Implemented / v1 policy | Implemented for the chosen v1 rules policy; intentionally unsupported sub-cases are documented and locked by tests. |
| Partial | Present, but still has known engine cleanup, timing, lifecycle, UI, or documentation work. |
| Missing | Not represented in `KnownCardIds` / default deck. |
| Needs UI | Headless logic exists, but the Web UI still needs a dedicated control/decision/targeting flow. |

## Current implementation snapshot

| Area | Status | Notes |
| --- | --- | --- |
| Default deck consistency | Implemented | Tests verify every `KnownCardIds` entry appears in the default deck and the deck contains no unknown ids. |
| Game event log | Implemented | `GameState` tracks `Version` and `EventLog`; tests cover command success/failure, version increments, event ordering, and returned/logged event identity. |
| Server-facing DTO projection | Implemented | `GameStateDtoMapper` maps game id, version, players, Tokyo, market, turn state, dice, flags, pending decisions, card counters, stored energy, scheduled turns, Opportunist reveal decisions, and Mimic targets. |
| DTO mapper coverage | Implemented | Mapper tests cover setup state, running turn, pending decisions, keep cards, card-local state, Mimic targets, status tokens, Tokyo slots, market cards/counts, dice, flags, and scheduled turns. |
| Representative game flow | Implemented | Integration tests cover turn lifecycle, Tokyo entry/leave/stay decisions, purchase, healing, scoring, start-in-Tokyo VP, versioning, and event log consistency. |
| Damage prevention/cancellation | Implemented | Static prevention, Wings cancellation, Camouflage random prevention, It Has a Child replacement, and lethal timing around Jets/Tokyo leave are covered. |
| Player status tokens | Implemented | Poison/shrink token state, attack-token application, heart-based token removal, shrink dice reduction, and poison end-of-turn damage are implemented/tested. |
| Owned keep-card lifecycle | Partial | Current production removal paths use lifecycle effects where needed, including Even Bigger add/loss. Remaining cleanup is architectural: a single generic owned-card lifecycle pipeline would reduce scattered cleanup calls. |
| Card-local counters / stored energy | Implemented / v1 policy | Smoke Cloud, Monster Batteries, Psychic Probe markers, Healing Ray spent-heart tracking, and Mimic target state exist. Stateful/self-discard Mimic activations are intentionally blocked for v1. |
| Extra-turn scheduling | Implemented | Freeze Time and Frenzy use scheduled extra turns; scheduled turns are exposed via DTO and consumed when turns begin. |
| Out-of-turn card activation | Implemented headless / Needs UI | Psychic Probe and Opportunist reaction windows are implemented headlessly; Opportunist UI is currently a known blocker. |
| Owned-card transfer/payment | Implemented headless / Needs UI | Parasitic Tentacles and Healing Ray payment flows exist. Parasitic Tentacles still needs Web UI. |
| Seating / adjacency effects | Implemented | Fire Breathing uses current alive player order and only adds damage to neighboring monsters that are already attack targets. |
| Forced dice result handling | Implemented | Background Dweller rerolls `Three` results during normal roll/reroll flow. No special UI should be needed. |
| Mimic copy infrastructure | Implemented / v1 policy / Needs UI polish | Mimic targeting, DTO state, retarget command/service/tests, passive copied effects, selected activated effects, stale-target cleanup, and unsupported stateful activation tests exist. UI timing is currently too permissive. |

## Card status table

| Card | Headless status | UI status | Notes |
| --- | --- | --- | --- |
| Acid Attack | Implemented | No special UI | +1 damage against other monsters for supported attack/card-effect damage sources; extensive combined coverage exists. |
| Alien Metabolism | Implemented | No special UI | Purchase discount. |
| Alpha Monster | Implemented | No special UI | VP when attacking. |
| Apartment Building | Implemented | No special UI beyond buy | Simple discard VP gain. |
| Armor Plating | Implemented | No special UI | Static damage reduction, including Acid Attack/Gas Refinery prevention coverage. |
| Background Dweller | Implemented | No special UI | Automatically rerolls `Three` results until none remain for the owning player's normal roll/reroll flow. If a purchase causes a stuck pending decision, check Opportunist UI. |
| Burrowing | Implemented | No special UI | Extra damage when attacking Tokyo and damage when leaving Tokyo; includes lethal replacement timing coverage. |
| Camouflage | Implemented | No special UI currently | Random prevention is automatic. Future UI may visualize prevention rolls. |
| Commuter Train | Implemented | No special UI beyond buy | Simple discard VP gain. |
| Complete Destruction | Implemented | No special UI | Bonus VP when scoring includes 1, 2, and 3. |
| Corner Store | Implemented | No special UI beyond buy | Simple discard VP gain. |
| Dedicated News Team | Implemented | No special UI | VP when buying cards, including Opportunist out-of-turn purchases. |
| Drop from High Altitude | Implemented | No special UI beyond buy | VP + enter Tokyo effect; Bay policy coverage exists. |
| Eater of the Dead | Implemented | No special UI | VP when a monster is eliminated, including multi-elimination, It Has a Child replacement, dead-owner exclusion, and all-eliminated timing coverage. |
| Energize | Implemented | No special UI beyond buy | Simple discard energy gain. |
| Energy Hoarder | Implemented | No special UI | End-turn VP from energy. |
| Even Bigger | Implemented | No special UI | Max-health add/loss behavior is centralized through `KeepCardLifecycleService` for current production paths. |
| Evacuation Orders | Implemented | No special UI beyond buy | Damage to all other monsters; Eater/It Has a Child coverage exists. |
| Extra Head | Implemented | No special UI | Extra die. |
| Fire Blast | Implemented | No special UI beyond buy | Damage to all other monsters; Acid Attack and Eater elimination coverage exists. |
| Fire Breathing | Implemented | No special UI | Adds +1 attack damage only to neighboring monsters that are already valid attack targets; 2-6 player and Bay coverage exists. |
| Freeze Time | Implemented | No special UI | Extra turn with one fewer die after scoring three 1s. |
| Frenzy | Implemented | No special UI beyond buy | Discard card that schedules an immediate extra turn after purchase. |
| Friend of Children | Implemented | No special UI | Bonus energy gain. |
| Gas Refinery | Implemented | No special UI beyond buy | VP + damage to all other monsters; prevention, Acid Attack, Wings, and Eater coverage exists. |
| Giant Brain | Implemented | No special UI | Extra reroll. |
| Gourmet | Implemented | No special UI | Bonus VP when scoring dice. |
| Heal | Implemented | No special UI beyond buy | Simple discard healing. |
| Healing Ray | Implemented | Partial | Main command dispatch, payment transfer, spent-heart tracking, and Mimic support are covered. Web UI has basic target/amount control but must verify remaining unused hearts. |
| Herbivore | Implemented | No special UI | End-turn VP when no damage dealt. |
| Herd Culler | Implemented | Needs UI | Activated once-per-turn die change to `One`; usable through valid Mimic copy. Needs die selection. |
| High Altitude Bombing | Implemented | No special UI beyond buy | Damage to everyone; Acid Attack/self-damage exclusion and victory timing coverage exists. |
| It Has a Child | Implemented | No special UI | Death replacement: discard owned cards, lose energy, heal to 10, leave Tokyo, still counts as elimination; Bay cleanup and Eater timing covered. |
| Jet Fighters | Implemented | No special UI beyond buy | VP + self damage. |
| Jets | Implemented | No special UI | Leave-Tokyo damage recovery, Wings interaction, Burrowing interaction, and lethal/prevention timing are covered. |
| Made in a Lab | Implemented | Partial | Peek and optionally buy top deck card; usable through valid Mimic copy. Web UI has partial flow and must be verified. |
| Metamorph | Implemented / v1 policy | Needs UI | Real-card activation works. Mimic activation is intentionally blocked for v1 because it is self-discard/stateful. Needs own keep-card selection at end of turn. |
| Mimic | Implemented / v1 policy | Partial | Supported copied effects work. UI action badge and target selection exist, but timing is currently too permissive. |
| Monster Batteries | Implemented | No special UI currently | Stored energy, payments, end-turn drain, discard when empty, and Mimic cleanup when discarded. Future UI should display stored energy clearly. |
| National Guard | Implemented | No special UI beyond buy | VP + self damage. |
| Nova Breath | Implemented | No special UI | Attack damages all other monsters regardless of Tokyo position. |
| Nuclear Power Plant | Implemented | No special UI beyond buy | VP + healing. |
| Omnivore | Implemented | No special UI | +2 VP when the owner rolls at least one pair; dice are not consumed; multiple pairs give one bonus; Mimic compatibility and stacking coverage exist. |
| Opportunist | Implemented headless | Blocking / Needs UI | Reacts to newly revealed market cards. API/client buy/decline support exists, but `GameTable.razor` still needs pending-decision buttons. |
| Parasitic Tentacles | Implemented headless | Needs UI | Transfer keep cards from another living player during purchase phase, pay seller, apply lifecycle effects, and clean Mimic targets. Needs action on other players' keep cards. |
| Plot Twist | Implemented / v1 policy | Needs UI | Real-card activation works and self-discards. Mimic activation is intentionally blocked for v1. Needs die + target face UI. |
| Poison Quills | Implemented | No special UI | Damage when scoring 1s; Acid Attack, Mimic, and Wings coverage exists. |
| Poison Spit | Implemented | No special UI | Adds poison tokens to damaged attack targets; poison end-turn damage and heart removal exist. |
| Psychic Probe | Implemented / v1 policy | Needs UI | Real out-of-turn activation works. Mimic activation is intentionally blocked for v1. Needs non-active-player UI during another player's rolling phase. |
| Rapid Healing | Implemented | Partial | Activated healing; usable through valid Mimic copy. Basic UI exists, timing should be verified. |
| Regeneration | Implemented | No special UI | Bonus healing. |
| Rooting for the Underdog | Implemented | No special UI | End-turn VP if tied/fewest VP. |
| Shrink Ray | Implemented | No special UI | Adds shrink tokens to damaged attack targets; shrink dice reduction and heart removal exist. |
| Skyscraper | Implemented | No special UI beyond buy | Simple discard VP gain. |
| Smoke Cloud | Implemented / v1 policy | Needs UI | Real counter-based activation works. Mimic activation is intentionally blocked for v1. Needs counter action UI. |
| Solar Powered | Implemented | No special UI | End-turn energy if empty. |
| Spiked Tail | Implemented | No special UI | Extra attack damage. |
| Stretchy | Implemented | Needs UI | Activated die face change; usable through valid Mimic copy. Needs die + target face UI. |
| Tanks | Implemented | No special UI beyond buy | VP + self damage. |
| Telepath | Implemented | Needs UI | Activated extra reroll; usable through valid Mimic copy. Needs keep-card action UI. |
| Urbavore | Implemented | No special UI | Tokyo start-turn VP and Tokyo damage bonus. |
| Vast Storm | Implemented | No special UI beyond buy | VP + damage based on opponents' energy; prevention, Wings, and Eater coverage exists. |
| We're Only Making It Stronger | Implemented / needs regression test | No special UI | Correct effect is energy when losing 2+ health. Recent fix changed `DamageApplier` to grant energy instead of VP; add regression test and rename misleading helper later. |
| Wings | Implemented | Partial | Explicit activation after damage taken this turn; spends 2 energy, cancels/heals damage, emits `DamageCanceledEvent`, and preserves/updates pending Tokyo leave decisions. Basic UI exists, timing UX needs verification. |

## Missing or incomplete cards from the uploaded card reference

No known card from the current represented/default deck is missing as headless logic.

Known intentional v1 policy exclusions are limited to Mimic activation of stateful/self-discard effects:

- Smoke Cloud
- Plot Twist
- Metamorph
- Psychic Probe

These are documented in `docs/MIMIC_POLICY_NOTES.md` and locked by `MimicUnsupportedStatefulCardFlowTests`.

## Remaining work

### Must-have now

- Keep full suite green.
- Finish `OpportunistPurchase` UI.
- Tighten Mimic UI timing.
- Browser-test all current partial UI cards.
- Add/update regression tests for all new special-card command/API/pending-decision work where practical.

### Nice-to-have / cleanup

- Rename misleading helper for We're Only Making It Stronger.
- Add explicit We're Only Making It Stronger regression tests, including Mimic copy.
- Refactor scattered card effect branches into effect handlers.
- Add a central owned-card lifecycle pipeline.
- Add API adapter tests for pending decisions.
- Add a direct invariant/unit test that a monster cannot occupy both City and Bay if not already enforced elsewhere.
- Decide whether event log should be bounded/snapshot-friendly before persistence.

## Recommended next steps

1. Run build/test.
2. Implement `OpportunistPurchase` UI.
3. Fix Mimic UI timing.
4. Continue the UI checklist in `docs/UI_CARD_ACTIONS_HANDOFF.md`.
