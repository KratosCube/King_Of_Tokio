# King of Tokyo engine - card implementation audit

This document tracks how far the headless engine is from supporting the full card set before building the online Blazor UI.

Last verified locally by user after Parasitic Tentacles flow-test and CardBoughtEvent cost alias fixes: `dotnet test KingOfTokyo.Engine.slnx` succeeded.

Source of truth for this audit:

- Current engine card ids in `KnownCardIds.cs`
- Current default market deck in `MarketSetupService.cs`
- Current card/effect integration tests under `tests/KingOfTokyo.Core.Tests`
- Uploaded card reference PDF/rules used during planning

The original card text is intentionally summarized here. The goal is implementation planning, not copying final public-facing card copy.

## Status legend

| Status | Meaning |
| --- | --- |
| Implemented | Present in the default deck and core effect appears to be supported by engine code/tests. |
| Partial | Present, but missing edge cases, loss effects, timing, UI decisions, or persistence support. |
| Missing | Not yet represented in `KnownCardIds` / default deck. |
| Needs engine concept | Requires a new generic engine mechanism before the card can be implemented cleanly. |

## Current implementation snapshot

The codebase now has stable foundations for the online UI boundary:

| Area | Status | Notes |
| --- | --- | --- |
| Default deck consistency | Implemented | Tests verify that every `KnownCardIds` entry appears in the default deck and that the deck does not contain unknown ids. |
| Game event log | Implemented | `GameState` tracks `Version` and `EventLog`; tests cover successful commands, failed commands, exact version increments, event ordering, and returned/logged event identity. |
| Server-facing DTO projection | Implemented | `GameStateDtoMapper` maps game id, version, players, Tokyo, market, turn state, dice, flags, pending decisions, card counters, stored energy, scheduled turns, and Opportunist reveal decisions. |
| DTO mapper coverage | Implemented | Mapper tests cover setup state, running turn, pending reroll decision payload, Opportunist purchase payload, keep cards, card-local state, status tokens, Tokyo slots, market cards/counts, dice, flags, and scheduled turns. |
| Representative game flow | Implemented | Integration test covers multiple turns, Tokyo entry/leave/stay decisions, card purchase, healing, scoring, start-in-Tokyo VP, versioning, and event log consistency. |
| Damage prevention/cancellation | Partial | Static prevention, Wings cancellation, Camouflage random prevention, and It Has a Child elimination replacement exist. Still needs more lethal/timing edge-case regression tests. |
| Player status tokens | Implemented | Poison/shrink token state exists; attack-token application, heart-based token removal, shrink dice reduction, and poison end-of-turn damage are covered. More edge cases can still be added as regression tests. |
| Owned keep-card lifecycle | Partial | Player-owned keep cards can be added/removed/discarded/transferred; Plot Twist, Metamorph, Smoke Cloud, Monster Batteries, Psychic Probe, Opportunist, Parasitic Tentacles, and It Has a Child use this. Generic lifecycle hooks are still missing. |
| Card-local counters / stored energy | Partial | Smoke Cloud counters, Monster Batteries stored energy, and Psychic Probe once-per-turn marker exist and are exposed through DTOs. Mimic/copying and generic lifecycle events are still missing. |
| Extra-turn scheduling | Implemented | Freeze Time and Frenzy use scheduled extra turns; scheduled turns are exposed through DTOs and consumed when beginning turns. |
| Out-of-turn card activation | Implemented | Psychic Probe can be used during another player's rolling phase. Opportunist can react to newly revealed market cards, decline, or buy the revealed card out of turn. |
| Owned-card transfer/payment | Partial | Parasitic Tentacles can transfer keep cards from another living player during purchase phase and pay the seller. Still needs richer event coverage and generic lifecycle hooks for transferred card effects. |
| Seating / adjacency effects | Implemented | Fire Breathing uses current alive player order to resolve neighbors and only adds damage to neighbors who are already attack targets. |
| Forced dice result handling | Implemented | Background Dweller rerolls rolled/rerolled `Three` results until none remain during the owning player's normal roll/reroll flow. |

## Currently represented in code

The current codebase represents these cards in `KnownCardIds` and `MarketSetupService`:

| Card | Status | Notes |
| --- | --- | --- |
| Acid Attack | Partial | Implemented as +1 attack damage when attack dice are rolled. Original wording also implies extra damage even when not attacking; confirm/extend timing. |
| Alien Metabolism | Implemented | Purchase discount. |
| Alpha Monster | Implemented | VP when attacking. |
| Apartment Building | Implemented | Simple discard VP gain. |
| Armor Plating | Implemented | Damage reduction. |
| Background Dweller | Implemented | Automatically rerolls `Three` results until none remain for the owning player's normal roll/reroll flow; covered by service and GameEngine flow tests. |
| Burrowing | Implemented | Extra damage when attacking Tokyo and damage when leaving Tokyo. |
| Camouflage | Implemented | Rolls one die per remaining incoming damage after static prevention; each heart prevents 1 damage. Covered by prevention tests and attack-flow test. |
| Commuter Train | Implemented | Simple discard VP gain. |
| Complete Destruction | Implemented | Bonus VP when scoring includes 1, 2, and 3. |
| Corner Store | Implemented | Simple discard VP gain. |
| Dedicated News Team | Implemented | VP when buying cards, including Opportunist out-of-turn purchases. |
| Drop from High Altitude | Partial | VP + enter Tokyo effect exists. Needs exact rule confirmation for forcing Tokyo control when occupied, especially with Bay. |
| Eater of the Dead | Implemented | VP when a monster is eliminated, including the It Has a Child replacement case. |
| Energize | Implemented | Simple discard energy gain. |
| Energy Hoarder | Implemented | End-turn VP from stored energy. |
| Even Bigger | Partial | Gain effect exists; loss effect is applied through Metamorph discard path. Still needs generic `OnCardLost` lifecycle for all future removal paths. |
| Evacuation Orders | Implemented | Damage to all other monsters. |
| Extra Head | Implemented | Extra die. |
| Fire Blast | Implemented | Damage to all other monsters. |
| Fire Breathing | Implemented | Adds +1 attack damage only to neighboring monsters that are already valid attack targets; does not create new damage packets for unattacked neighbors. |
| Freeze Time | Implemented | Extra turn with one fewer die after scoring three 1s. Uses scheduled extra-turn queue and has integration coverage. |
| Frenzy | Implemented | Discard card that schedules an immediate extra turn after purchase. |
| Friend of Children | Implemented | Bonus energy gain. |
| Gas Refinery | Implemented | VP + damage to all other monsters. |
| Giant Brain | Implemented | Extra reroll through keep-card rule. |
| Gourmet | Implemented | Bonus VP when scoring dice. |
| Heal | Implemented | Simple discard healing. |
| Herbivore | Implemented | End-turn VP when no damage dealt. |
| Herd Culler | Implemented | Activated once-per-turn die change to `One`. |
| High Altitude Bombing | Implemented | Damage to everyone. |
| It Has a Child | Implemented | Death replacement: discards all owned keep cards, loses all energy, heals to 10, leaves Tokyo, and still counts as elimination for Eater of the Dead. |
| Jet Fighters | Implemented | VP + self damage. |
| Jets | Partial | Leave-Tokyo damage recovery is represented and Wings can zero the pending leave damage. Still needs edge-case check for prevention timing versus lethal damage. |
| Made in a Lab | Implemented | Peek and optionally buy top deck card through pending decision. |
| Metamorph | Implemented | Activated in purchase phase; discards an owned keep card and grants energy equal to cost. |
| Monster Batteries | Implemented | Starts with stored energy, can pay card/refresh costs from stored energy, drains at end of turn, and discards when empty. |
| National Guard | Implemented | VP + self damage. |
| Nova Breath | Implemented | Attack damages all other monsters regardless of Tokyo position. |
| Nuclear Power Plant | Implemented | VP + healing. |
| Opportunist | Implemented | Reacts to newly revealed market cards after purchase/refresh; eligible player can decline or buy the revealed card out of turn. DTO coverage exists for the pending decision payload. |
| Parasitic Tentacles | Implemented | Current player with this keep card can buy keep cards from another living player during purchase phase, transferring the card and paying the seller the card cost in energy. Covered by service and GameEngine flow tests. |
| Plot Twist | Implemented | One-use die result change, then discards itself. |
| Poison Quills | Implemented | Damage when scoring 1s. |
| Poison Spit | Implemented | Adds poison tokens to damaged attack targets; poison end-of-turn damage and heart-based removal are implemented/tested. |
| Psychic Probe | Implemented | Out-of-turn activation during another player's rolling phase; rerolls one die, discards on energy, and is limited to once during each other player's turn. |
| Rapid Healing | Implemented | Activated keep-card healing. |
| Regeneration | Implemented | Bonus healing. |
| Rooting for the Underdog | Implemented | End-turn VP if tied/fewest VP; keep tie behavior covered/confirmed. |
| Shrink Ray | Implemented | Adds shrink tokens to damaged attack targets; shrink tokens reduce dice count and hearts can remove tokens. |
| Skyscraper | Implemented | Simple discard VP gain. |
| Smoke Cloud | Implemented | Starts with charges, spends one charge for an extra reroll, and discards itself when exhausted. |
| Solar Powered | Implemented | End-turn energy if empty. |
| Spiked Tail | Implemented | Extra attack damage. |
| Stretchy | Implemented | Activated die face change. |
| Tanks | Implemented | VP + self damage. |
| Telepath | Implemented | Activated extra reroll. |
| Urbavore | Implemented | Tokyo start-turn VP and Tokyo damage bonus. |
| Vast Storm | Implemented | VP + damage based on opponents' energy. |
| We're Only Making It Stronger | Implemented | VP when losing 2+ health. |
| Wings | Implemented | Explicit activation after damage taken this turn; spends 2 energy, cancels/heals that damage, emits `DamageCanceledEvent`, and preserves/updates pending Tokyo leave decisions. |

## Missing or incomplete cards from the uploaded card reference

| Card | Status | Main engine need |
| --- | --- | --- |
| Healing Ray | Missing / Needs engine concept | Heal other monsters using healing dice and transfer energy/payment. |
| Mimic | Missing / Needs engine concept | Copy another keep card; retarget by spending energy. |
| Omnivore | Missing / Needs engine concept | Special scoring with pairs; dice can still be used in other combinations. |

## Remaining pure-engine work before UI

### Must-have before online UI

- Finish the remaining missing/incomplete card mechanics that affect public game state.
- Add card copy/lifecycle concepts for Mimic and refine generic transfer hooks for Parasitic Tentacles / Healing Ray.
- Add end-to-end tests around elimination, victory timing, and a longer representative full-game path.
- Keep DTO/event-log coverage synchronized whenever new state is introduced.

### Nice-to-have before UI

- Refactor card effects out of the growing `KeepCardRulesService` / `GameEngine` branches into effect handlers.
- Add snapshot/event cursor DTOs for online resync.
- Add more regression tests for Bay edge cases and simultaneous eliminations.
- Decide whether event log should be bounded/snapshot-friendly before persistence.

## Recommended engine concepts to add next

### 1. Owned-card lifecycle and transfer hooks

Needed for Even Bigger follow-ups, Mimic, Healing Ray, and future richer keep cards.

Required operations:

- Add keep card
- Remove keep card
- Discard keep card
- Transfer keep card
- Run `OnCardLost` / `OnCardDiscarded` effects from every removal path
- Recalculate copied / transferred effects consistently

### 2. Special scoring extensions

Needed for Omnivore.

Required capabilities:

- score special combinations without consuming dice that other scoring rules need,
- keep scoring tests deterministic.

## Proposed implementation order

1. Implement Healing Ray healing/payment flow.
2. Implement Mimic copy/retarget behavior.
3. Implement Omnivore scoring extension.
4. Add one longer end-to-end full-game regression flow.
5. Start SignalR server and Blazor client only after the headless engine can complete representative games.
