# King of Tokyo engine - card implementation audit

This document tracks the current headless card-logic status before building the online UI/server layer.

Last status update: Mimic v1 policy closure and Omnivore audit cleanup.

Validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Status legend

| Status | Meaning |
| --- | --- |
| Implemented | Present in the default deck and core effect is supported by engine code/tests. |
| Implemented / v1 policy | Implemented for the chosen v1 rules policy; intentionally unsupported sub-cases are documented and locked by tests. |
| Partial | Present, but still has known engine cleanup, timing, lifecycle, or documentation work. |
| Missing | Not represented in `KnownCardIds` / default deck. |
| Needs engine concept | Requires a new generic engine mechanism before implementation. |

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
| Out-of-turn card activation | Implemented | Psychic Probe and Opportunist reaction windows are implemented and covered. |
| Owned-card transfer/payment | Partial | Parasitic Tentacles and Healing Ray payment flows exist. Remaining cleanup is centralizing lifecycle/Mimic cleanup hooks. |
| Seating / adjacency effects | Implemented | Fire Breathing uses current alive player order and only adds damage to neighboring monsters that are already attack targets. |
| Forced dice result handling | Implemented | Background Dweller rerolls `Three` results during normal roll/reroll flow. |
| Mimic copy infrastructure | Implemented / v1 policy | Mimic targeting, DTO state, retarget command/service/tests, passive copied effects, selected activated effects, stale-target cleanup, and unsupported stateful activation tests exist. |

## Card status table

| Card | Status | Notes |
| --- | --- | --- |
| Acid Attack | Implemented | +1 damage against other monsters for supported attack/card-effect damage sources; extensive combined coverage exists. |
| Alien Metabolism | Implemented | Purchase discount. |
| Alpha Monster | Implemented | VP when attacking. |
| Apartment Building | Implemented | Simple discard VP gain. |
| Armor Plating | Implemented | Static damage reduction, including Acid Attack/Gas Refinery prevention coverage. |
| Background Dweller | Implemented | Automatically rerolls `Three` results until none remain for the owning player's normal roll/reroll flow. |
| Burrowing | Implemented | Extra damage when attacking Tokyo and damage when leaving Tokyo; includes lethal replacement timing coverage. |
| Camouflage | Implemented | Rolls one die per remaining incoming damage after static prevention; each heart prevents 1 damage. |
| Commuter Train | Implemented | Simple discard VP gain. |
| Complete Destruction | Implemented | Bonus VP when scoring includes 1, 2, and 3. |
| Corner Store | Implemented | Simple discard VP gain. |
| Dedicated News Team | Implemented | VP when buying cards, including Opportunist out-of-turn purchases. |
| Drop from High Altitude | Implemented | VP + enter Tokyo effect; Bay policy coverage exists. |
| Eater of the Dead | Implemented | VP when a monster is eliminated, including multi-elimination, It Has a Child replacement, dead-owner exclusion, and all-eliminated timing coverage. |
| Energize | Implemented | Simple discard energy gain. |
| Energy Hoarder | Implemented | End-turn VP from energy. |
| Even Bigger | Implemented | Max-health add/loss behavior is centralized through `KeepCardLifecycleService` for current production paths. |
| Evacuation Orders | Implemented | Damage to all other monsters; Eater/It Has a Child coverage exists. |
| Extra Head | Implemented | Extra die. |
| Fire Blast | Implemented | Damage to all other monsters; Acid Attack and Eater elimination coverage exists. |
| Fire Breathing | Implemented | Adds +1 attack damage only to neighboring monsters that are already valid attack targets; 2-6 player and Bay coverage exists. |
| Freeze Time | Implemented | Extra turn with one fewer die after scoring three 1s. |
| Frenzy | Implemented | Discard card that schedules an immediate extra turn after purchase. |
| Friend of Children | Implemented | Bonus energy gain. |
| Gas Refinery | Implemented | VP + damage to all other monsters; prevention, Acid Attack, Wings, and Eater coverage exists. |
| Giant Brain | Implemented | Extra reroll. |
| Gourmet | Implemented | Bonus VP when scoring dice. |
| Heal | Implemented | Simple discard healing. |
| Healing Ray | Implemented | Main command dispatch, typed extension compatibility, payment transfer, spent-heart tracking, and Mimic support are covered. |
| Herbivore | Implemented | End-turn VP when no damage dealt. |
| Herd Culler | Implemented | Activated once-per-turn die change to `One`; usable through valid Mimic copy. |
| High Altitude Bombing | Implemented | Damage to everyone; Acid Attack/self-damage exclusion and victory timing coverage exists. |
| It Has a Child | Implemented | Death replacement: discard owned cards, lose energy, heal to 10, leave Tokyo, still counts as elimination; Bay cleanup and Eater timing covered. |
| Jet Fighters | Implemented | VP + self damage. |
| Jets | Implemented | Leave-Tokyo damage recovery, Wings interaction, Burrowing interaction, and lethal/prevention timing are covered. |
| Made in a Lab | Implemented | Peek and optionally buy top deck card; usable through valid Mimic copy. |
| Metamorph | Implemented / v1 policy | Real-card activation works. Mimic activation is intentionally blocked for v1 because it is self-discard/stateful. |
| Mimic | Implemented / v1 policy | Supported copied effects work. Smoke Cloud, Plot Twist, Metamorph, and Psychic Probe activations via Mimic are intentionally blocked for v1 and locked by tests. |
| Monster Batteries | Implemented | Stored energy, payments, end-turn drain, discard when empty, and Mimic cleanup when discarded. |
| National Guard | Implemented | VP + self damage. |
| Nova Breath | Implemented | Attack damages all other monsters regardless of Tokyo position. |
| Nuclear Power Plant | Implemented | VP + healing. |
| Omnivore | Implemented | +2 VP when the owner rolls at least one pair; dice are not consumed; multiple pairs give one bonus; Mimic compatibility and stacking coverage exist. |
| Opportunist | Implemented | Reacts to newly revealed market cards after purchase/refresh; decline and out-of-turn buy flows exist. |
| Parasitic Tentacles | Implemented | Transfer keep cards from another living player during purchase phase, pay seller, apply lifecycle effects, and clean Mimic targets. |
| Plot Twist | Implemented / v1 policy | Real-card activation works and self-discards. Mimic activation is intentionally blocked for v1. |
| Poison Quills | Implemented | Damage when scoring 1s; Acid Attack, Mimic, and Wings coverage exists. |
| Poison Spit | Implemented | Adds poison tokens to damaged attack targets; poison end-turn damage and heart removal exist. |
| Psychic Probe | Implemented / v1 policy | Real out-of-turn activation works. Mimic activation is intentionally blocked for v1 because it uses once-per-turn/self-discard state. |
| Rapid Healing | Implemented | Activated healing; usable through valid Mimic copy. |
| Regeneration | Implemented | Bonus healing. |
| Rooting for the Underdog | Implemented | End-turn VP if tied/fewest VP. |
| Shrink Ray | Implemented | Adds shrink tokens to damaged attack targets; shrink dice reduction and heart removal exist. |
| Skyscraper | Implemented | Simple discard VP gain. |
| Smoke Cloud | Implemented / v1 policy | Real counter-based activation works. Mimic activation is intentionally blocked for v1. |
| Solar Powered | Implemented | End-turn energy if empty. |
| Spiked Tail | Implemented | Extra attack damage. |
| Stretchy | Implemented | Activated die face change; usable through valid Mimic copy. |
| Tanks | Implemented | VP + self damage. |
| Telepath | Implemented | Activated extra reroll; usable through valid Mimic copy. |
| Urbavore | Implemented | Tokyo start-turn VP and Tokyo damage bonus. |
| Vast Storm | Implemented | VP + damage based on opponents' energy; prevention, Wings, and Eater coverage exists. |
| We're Only Making It Stronger | Implemented | VP when losing 2+ health. |
| Wings | Implemented | Explicit activation after damage taken this turn; spends 2 energy, cancels/heals damage, emits `DamageCanceledEvent`, and preserves/updates pending Tokyo leave decisions. Usable through valid Mimic copy. |

## Missing or incomplete cards from the uploaded card reference

No known card from the current represented/default deck is missing as headless logic.

Known intentional v1 policy exclusions are limited to Mimic activation of stateful/self-discard effects:

- Smoke Cloud
- Plot Twist
- Metamorph
- Psychic Probe

These are documented in `docs/MIMIC_POLICY_NOTES.md` and locked by `MimicUnsupportedStatefulCardFlowTests`.

## Remaining pure-engine work before UI

### Must-have before online UI

- Keep the full suite green.
- Finish/confirm owned-card lifecycle cleanup review.
- Decide whether an event cursor / incremental sync helper is needed before server work.
- Keep DTO/event-log coverage synchronized whenever new state is introduced.

### Nice-to-have before UI

- Refactor scattered card effect branches into effect handlers.
- Add snapshot/event cursor DTOs for online resync if the server API needs them.
- Add a direct invariant/unit test that a monster cannot occupy both City and Bay if not already enforced elsewhere.
- Decide whether event log should be bounded/snapshot-friendly before persistence.

## Recommended next steps

1. Run the full test suite and keep it green.
2. Review direct `RemoveKeepCard(...)` paths and either confirm current lifecycle handling or centralize it.
3. Decide whether event cursor/incremental sync is needed before starting the server layer.
4. Update `ENGINE_HANDOFF.md` before switching to UI/server work.
