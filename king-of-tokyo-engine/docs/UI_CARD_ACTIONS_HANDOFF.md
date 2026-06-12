# UI card actions handoff

This document tracks the current Blazor/Web UI work for special King of Tokyo cards.

Current branch:

```text
rollback-before-dev-player-control
```

Current app path:

```text
king-of-tokyo-engine
```

Primary validation commands from repository root:

```bash
git pull
dotnet build king-of-tokyo-engine/KingOfTokyo.Engine.slnx
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Current UI direction

The current UX target is:

- card image click opens/keeps card detail behavior,
- card actions are triggered from a small `Action` badge/button under the keep card,
- one selected keep card opens a contextual action panel near the dice/Tokyo area,
- target selection should happen by clicking relevant player/keep-card/die UI, not by rendering huge global button lists,
- no JS should mutate Blazor-rendered game state DOM.

Important files:

```text
src/KingOfTokyo.Web/Pages/GameTable.razor
src/KingOfTokyo.Web/Components/PlayerKeepCards.razor
src/KingOfTokyo.Web/Services/ApiClient.cs
src/KingOfTokyo.Web/Contracts/ApiContracts.cs
src/KingOfTokyo.Web/wwwroot/css/local-card-layout.css
src/KingOfTokyo.Web/wwwroot/js/dice-drag-select.js
src/KingOfTokyo.Api/Endpoints/GameEndpoints.cs
src/KingOfTokyo.Api/Contracts/ApiDtos.cs
```

## Recently changed UI foundation

Recent UI changes:

- dice face DOM mutation was removed from `dice-drag-select.js`, so dice contents are owned by Blazor,
- keep card component now accepts owner id and returns `(ownerPlayerId, card)` on action click,
- card art/detail click and card action click were separated,
- keep card `Action` badge is now a real button,
- basic MVP keep-card action panel exists in `GameTable.razor`,
- `DieIcon(...)` maps `HEART` / `HEAL` to `❤` and `ATTACK` / `CLAW` to `💥`.

Recent relevant commits:

```text
0cb2ae235d9e76dbc7a5aa49086ab723ce41a64e  Stop mutating dice face DOM from JavaScript
708c8faba01bebd382335e51d4ab584621077ab5  Pass keep card owner to click handlers
7d3f8586ca99ea61a44ce6bb5afec5f92c4109a7  Drive MVP keep card actions from card clicks
216ad7d69dd07317efa1b8ee7d34e240e2272cb1  Use action badge for keep card actions
9cdf2342b2a7e6530d3cad9e17cd25bbf22b5108  Style keep card action badges as buttons
```

## Current card UI checklist

Legend:

- Done: usable enough in Web UI.
- Partial: some plumbing/UI exists, but missing timing, decision UI, or validation.
- Missing: engine/API may exist, but UI is not usable.

| Card / UI concept | UI status | Notes / next action |
| --- | --- | --- |
| Normal market buy/refresh | Done | Basic market buttons exist. |
| Dice roll/reroll/finalize | Done / needs polish | Blazor owns dice labels; JS only handles drag select. Dice action buttons may still be split between dice panel and next action panel. |
| Made in a Lab | Partial | API/client support exists; keep-card action exists; top card reveal is hidden from opponents and shown only to decision actor. Verify card image and buy/skip flow. |
| Rapid Healing | Partial | Basic keep-card action exists. Verify timing: card says any time during your turn; UI currently leans on purchase/action availability. |
| Wings | Partial | Basic keep-card action exists. Needs timing UX after damage and pending Tokyo leave decisions. |
| Healing Ray | Partial | Basic target/amount UI exists. Must verify it uses remaining unused heart dice, not only total hearts. |
| Mimic | Partial | Basic `Action` badge and click target selection exist. UI timing is too permissive; fix it next. |
| Opportunist | Blocking / Partial | Engine/API buy flow exists; API decline endpoint and Web client methods were added. `GameTable.razor` still needs `OpportunistPurchase` pending-decision UI with `Buy revealed card` / `Skip`. |
| Telepath | Missing | Needs keep-card action: spend 1 energy, gain extra reroll / pending reroll decision. |
| Stretchy | Missing | Needs die-selection + target face UI. Can share dice-face selector with Plot Twist. |
| Herd Culler | Missing | Needs die-selection UI to change a die to `One`; once-per-turn visibility. |
| Smoke Cloud | Missing | Needs action for real card counters only. Mimic activation intentionally blocked by v1 policy. |
| Plot Twist | Missing | Needs die-selection + target face UI, then self-discard. Mimic activation intentionally blocked by v1 policy. |
| Metamorph | Missing | Needs own-keep-card selection at end of turn, then discard selected keep card for energy. Mimic activation intentionally blocked by v1 policy. |
| Psychic Probe | Missing | Needs out-of-turn UI for non-active player to reroll one active player's die once. Mimic activation intentionally blocked by v1 policy. |
| Parasitic Tentacles | Missing | Needs buy action on other players' keep cards during purchase phase, with payment to seller. |
| Background Dweller | No special UI needed | Automatic on roll/reroll. If a purchase appears to create a stuck pending decision, check Opportunist. |
| We're Only Making It Stronger | No special UI needed | Automatic. Recent engine bug fixed: should grant energy when losing 2+ HP, not VP. Needs regression test. |
| Passive keep cards | No special UI needed | Need visible card state/event feed only. |
| Discard cards | No special UI beyond market buy | Effects resolve on purchase. |

## Current known blocker: OpportunistPurchase UI

Symptom:

- user buys a card,
- a new market card is revealed,
- if an eligible player has Opportunist, engine sets `PendingDecision.DecisionType == "OpportunistPurchase"`,
- UI currently shows pending decision text but no buttons, so the game feels stuck.

Already done:

- `GameEngine` supports `BuyOpportunistRevealedCardCommand` and `DeclineOpportunistRevealedCardCommand`,
- `GameEndpoints` now has endpoint for `decline-opportunist-revealed-card`,
- `ApiClient` now exposes:
  - `BuyOpportunistRevealedCardAsync(...)`,
  - `DeclineOpportunistRevealedCardAsync(...)`.

Still needed in `GameTable.razor`:

- detect `PendingDecision.DecisionType == "OpportunistPurchase"`,
- parse payload fields:
  - `slotIndex`,
  - `cardId`,
  - `cardName`,
  - `cost`,
  - `eligiblePlayerIds`,
- if local player is decision actor, show:
  - `Buy revealed card`,
  - `Skip`,
- call:
  - `ApiClient.BuyOpportunistRevealedCardAsync(GameId, new ActorRequest(Session.PlayerId))`,
  - `ApiClient.DeclineOpportunistRevealedCardAsync(GameId, new ActorRequest(Session.PlayerId))`.

Potential UI can mirror Made in a Lab's pending card layout, but Opportunist's payload currently does not include `description` or `cardType`. The UI can show card image from `cardId`, name, cost, and a short description like “Newly revealed market card.”

## Mimic UI timing policy

The engine should remain the source of truth, but UI should not offer illegal actions.

Correct UI behavior:

- A real Mimic card with no target can select an initial target after purchase / during the current purchase phase.
- A real Mimic card with a target can retarget only at the start of its owner's turn before rolling, and only if the player can pay the required energy.
- If Mimic is copying a supported action card, the Mimic card should show the copied action when that copied action is legal.
- Mimic should not offer UI actions for copied stateful/self-discard cards blocked by v1 policy:
  - Smoke Cloud,
  - Plot Twist,
  - Metamorph,
  - Psychic Probe.

Current issue:

- `GameTable.razor` currently uses `SelectedCanUseMimic => SelectedActionCard?.CardId == MimicCardId && MimicTargetOptions.Any()` which is too broad.
- `IsKeepCardActionAvailable(...)` also offers Mimic too broadly.

Suggested fix:

- add something like `CanChooseMimicTarget` and use it for real Mimic target selection,
- separate “Mimic target selection action” from “Mimic copied card action”.

## Healing Ray UI notes

Engine rule:

- Healing Ray can be used after dice are resolved and before turn ends,
- it spends unused heart dice tracked by `TurnState.HealingRayHeartsSpent`,
- target pays 2 energy per healed HP, or all remaining energy if unable to pay enough.

Current UI issue:

- `GameTable.razor` computes `AvailableHealingRayHearts` by counting all heart dice.
- It should account for already spent Healing Ray hearts if DTO exposes that state; if not, add it to DTO or infer conservatively.

## Recommended next work order

1. Run build/test locally.
2. Fix any compile/test failures.
3. Implement `OpportunistPurchase` UI in `GameTable.razor`.
4. Tighten Mimic UI timing.
5. Verify Made in a Lab, Opportunist, Mimic, Rapid Healing, Wings, Healing Ray manually in browser.
6. Add/adjust tests for API/DTO behavior around pending decisions where possible.
7. Implement the remaining UI cards in small batches:
   - Telepath + Smoke Cloud,
   - Herd Culler + Stretchy + Plot Twist shared die-face UI,
   - Metamorph + Parasitic Tentacles own/other keep-card selection,
   - Psychic Probe out-of-turn UI.
8. Create/maintain browser/manual checklist for every UI-relevant card.
