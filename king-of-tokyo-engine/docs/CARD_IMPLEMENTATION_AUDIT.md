# King of Tokyo engine - card implementation audit

This document tracks how far the headless engine is from supporting the full card set before building the online Blazor UI.

Last verified locally by user after representative game flow fix: `dotnet test KingOfTokyo.Engine.slnx` => 144 tests succeeded.

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
| Server-facing DTO projection | Implemented | `GameStateDtoMapper` maps game id, version, players, Tokyo, market, turn state, dice, flags, and pending decisions. |
| DTO mapper coverage | Implemented | Mapper tests cover setup state, running turn, pending reroll decision payload, keep cards, status tokens, Tokyo slots, market cards/counts, dice, and flags. |
| Representative game flow | Implemented | Integration test covers multiple turns, Tokyo entry/leave/stay decisions, card purchase, healing, scoring, start-in-Tokyo VP, versioning, and event log consistency. |
| Player status tokens | Implemented | Poison/shrink token state exists; attack-token application, heart-based token removal, shrink dice reduction, and poison end-of-turn damage are covered. More edge cases can still be added as regression tests. |
| Owned keep-card lifecycle | Partial | Player-owned keep cards can be added/removed/discarded; Plot Twist and Metamorph use this. Generic lifecycle hooks/transfers/counters are still missing. |

## Currently represented in code

The current codebase represents these cards in `KnownCardIds` and `MarketSetupService`:

| Card | Status | Notes |
| --- | --- | --- |
| Acid Attack | Partial | Implemented as +1 attack damage when attack dice are rolled. Original wording also implies extra damage even when not attacking; confirm/extend timing. |
| Alien Metabolism | Implemented | Purchase discount. |
| Alpha Monster | Implemented | VP when attacking. |
| Apartment Building | Implemented | Simple discard VP gain. |
| Armor Plating | Implemented | Damage reduction. |
| Burrowing | Implemented | Extra damage when attacking Tokyo and damage when leaving Tokyo. |
| Commuter Train | Implemented | Simple discard VP gain. |
| Complete Destruction | Implemented | Bonus VP when scoring includes 1, 2, and 3. |
| Corner Store | Implemented | Simple discard VP gain. |
| Dedicated News Team | Implemented | VP when buying cards. |
| Drop from High Altitude | Partial | VP + enter Tokyo effect exists. Needs exact rule confirmation for forcing Tokyo control when occupied, especially with Bay. |
| Eater of the Dead | Implemented | VP when a monster is eliminated. |
| Energize | Implemented | Simple discard energy gain. |
| Energy Hoarder | Implemented | End-turn VP from stored energy. |
| Even Bigger | Partial | Gain effect exists; loss effect is applied through Metamorph discard path. Still needs generic `OnCardLost` lifecycle for all future removal paths. |
| Evacuation Orders | Implemented | Damage to all other monsters. |
| Extra Head | Implemented | Extra die. |
| Fire Blast | Implemented | Damage to all other monsters. |
| Friend of Children | Implemented | Bonus energy gain. |
| Gas Refinery | Implemented | VP + damage to all other monsters. |
| Giant Brain | Implemented | Extra reroll through keep-card rule. |
| Gourmet | Implemented | Bonus VP when scoring dice. |
| Heal | Implemented | Simple discard healing. |
| Herbivore | Implemented | End-turn VP when no damage dealt. |
| Herd Culler | Implemented | Activated once-per-turn die change to `One`. |
| High Altitude Bombing | Implemented | Damage to everyone. |
| Jet Fighters | Implemented | VP + self damage. |
| Jets | Partial | Leave-Tokyo damage recovery is represented. Needs edge-case check for prevention timing versus lethal damage. |
| Made in a Lab | Implemented | Peek and optionally buy top deck card through pending decision. |
| Metamorph | Implemented | Activated in purchase phase; discards an owned keep card and grants energy equal to cost. |
| National Guard | Implemented | VP + self damage. |
| Nova Breath | Implemented | Attack damages all other monsters regardless of Tokyo position. |
| Nuclear Power Plant | Implemented | VP + healing. |
| Plot Twist | Implemented | One-use die result change, then discards itself. |
| Poison Quills | Implemented | Damage when scoring 1s. |
| Poison Spit | Implemented | Adds poison tokens to damaged attack targets; poison end-of-turn damage and heart-based removal are implemented/tested. |
| Rapid Healing | Implemented | Activated keep-card healing. |
| Regeneration | Implemented | Bonus healing. |
| Rooting for the Underdog | Implemented | End-turn VP if tied/fewest VP; keep tie behavior covered/confirmed. |
| Shrink Ray | Implemented | Adds shrink tokens to damaged attack targets; shrink tokens reduce dice count and hearts can remove tokens. |
| Skyscraper | Implemented | Simple discard VP gain. |
| Solar Powered | Implemented | End-turn energy if empty. |
| Spiked Tail | Implemented | Extra attack damage. |
| Stretchy | Implemented | Activated die face change. |
| Tanks | Implemented | VP + self damage. |
| Telepath | Implemented | Activated extra reroll. |
| Urbavore | Implemented | Tokyo start-turn VP and Tokyo damage bonus. |
| Vast Storm | Implemented | VP + damage based on opponents' energy. |
| We're Only Making It Stronger | Implemented | VP when losing 2+ health. |

## Missing or incomplete cards from the uploaded card reference

| Card | Status | Main engine need |
| --- | --- | --- |
| Opportunist | Missing / Needs engine concept | Reaction to newly revealed market card; out-of-turn purchase window. |
| Background Dweller | Missing | Always reroll a specific result; needs dice modification hook or card-specific reroll rule. |
| Camouflage | Missing / Needs engine concept | Roll per incoming damage point to prevent damage. Needs prevention hook and random roll. |
| Fire Breathing | Missing / Needs engine concept | Neighbor damage when dealing damage; needs seating/adjacency model. |
| Freeze Time | Missing / Needs engine concept | Extra turn with one fewer die after scoring 1s. |
| Frenzy | Missing / Needs engine concept | Immediate extra turn after purchase. |
| Healing Ray | Missing / Needs engine concept | Heal other monsters using healing dice and transfer energy/payment. |
| It Has a Child | Missing / Needs engine concept | Death replacement: discard cards, lose energy, reset to 10 health. |
| Mimic | Missing / Needs engine concept | Copy another keep card; retarget by spending energy. |
| Monster Batteries | Missing / Needs engine concept | Store energy on card and drain 2 energy per turn, then discard. |
| Omnivore | Missing / Needs engine concept | Special scoring with pairs; dice can still be used in other combinations. |
| Parasitic Tentacles | Missing / Needs engine concept | Buy cards from other players. Needs ownership transfer and payment to another player. |
| Psychic Probe | Missing / Needs engine concept | Reroll one die during another player's turn; discard on heart result. |
| Smoke Cloud | Missing / Needs engine concept | Charge counter card, spend charges for extra rerolls, auto-discard. |
| Wings | Missing / Needs engine concept | Spend energy to cancel damage during a turn. Needs prevention window. |

## Remaining pure-engine work before UI

### Must-have before online UI

- Finish the missing/incomplete card mechanics that affect public game state.
- Add generic damage prevention/replacement support for Wings, Camouflage, Jets edge cases, Armor Plating generalization, and It Has a Child.
- Add card-local state for charged/stored/copying cards: Smoke Cloud, Monster Batteries, Mimic.
- Add extra-turn scheduling for Freeze Time and Frenzy.
- Add out-of-turn reaction windows for Opportunist and Psychic Probe.
- Add seating/adjacency model for Fire Breathing.
- Add end-to-end tests around elimination, victory timing, and a longer representative full-game path.
- Keep DTO/event-log coverage synchronized whenever new state is introduced.

### Nice-to-have before UI

- Refactor card effects out of the growing `KeepCardRulesService` into effect handlers.
- Add snapshot/event cursor DTOs for online resync.
- Add more regression tests for Bay edge cases and simultaneous eliminations.
- Decide whether event log should be bounded/snapshot-friendly before persistence.

## Recommended engine concepts to add next

### 1. Damage prevention/replacement windows

Needed for Wings, Camouflage, Jets edge cases, Armor Plating generalization, and It Has a Child.

Recommended shape:

- Build a `DamageContext` before applying health changes.
- Let prevention/replacement effects modify or cancel damage.
- Only then apply damage and emit events.
- Keep Tokyo-leave decisions based on final applied attack damage.

### 2. Card-local state

Needed for Mimic, Smoke Cloud, Monster Batteries, and future richer keep cards.

Required capabilities:

- attach counters/tokens/energy to a card,
- expose those values through DTOs,
- spend/decrement those values through commands,
- auto-discard when exhausted,
- keep event log entries for card-local state changes.

### 3. Owned-card lifecycle

Needed for Even Bigger, Metamorph follow-ups, Mimic, Smoke Cloud, Plot Twist-style one-shot cards, Monster Batteries, and Parasitic Tentacles.

Required operations:

- Add keep card
- Remove keep card
- Discard keep card
- Transfer keep card
- Run `OnCardLost` / `OnCardDiscarded` effects from every removal path

### 4. Extra-turn scheduling

Needed for Freeze Time and Frenzy.

This should avoid mutating `CurrentPlayerIndex` ad hoc and instead give the turn coordinator an explicit queue/override for the next actor.

### 5. Out-of-turn reactions

Needed for Opportunist and Psychic Probe.

This should probably reuse `PendingDecision`, but support multiple eligible players and timeouts once online.

### 6. Seating/adjacency model

Needed for Fire Breathing.

A minimal model can be player order based, but tests should cover eliminated players and wrap-around neighbors.

## Proposed implementation order

1. Add a minimal damage prevention/replacement mechanism.
2. Implement Wings on top of the prevention mechanism.
3. Implement Camouflage on top of the prevention mechanism.
4. Add card-local counters/energy storage.
5. Implement Smoke Cloud.
6. Implement Monster Batteries.
7. Add extra-turn scheduling support.
8. Implement Freeze Time and Frenzy.
9. Add out-of-turn reaction support.
10. Implement Opportunist and Psychic Probe.
11. Add seating/adjacency support and implement Fire Breathing.
12. Implement/verify remaining card-transfer/copy cards: Mimic, Parasitic Tentacles, Healing Ray, It Has a Child, Omnivore, Background Dweller.
13. Start SignalR server and Blazor client only after the headless engine can complete representative games.
