# King of Tokyo / Vládce Tokia engine handoff

This document is the current handoff for continuing the headless King of Tokyo / Vládce Tokia engine before building any UI.

Repository:

```text
https://github.com/KratosCube/King_Of_Tokio/tree/main/king-of-tokyo-engine
```

Use only this project path:

```text
king-of-tokyo-engine
```

There have been accidental typo paths in the past such as `king-of-tokio-engine`. Do not add new files there. If such files appear in search results, treat them as stale unless they also exist under `king-of-tokyo-engine`.

Primary validation command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

If there is any doubt, start by running the full test suite locally.

## Work style to continue

- Keep changes small to medium.
- Prefer one safe logical step per commit.
- If production code is added, add or update tests in the same step.
- Keep the headless engine green before moving on.
- If something breaks, fix that first.
- Do not work on UI yet.
- Prefer existing command pattern, services, validators, domain state, DTOs, and events.
- After every change, report what changed, commit SHA, and the local command to run.
- Be careful with `GameEngine.cs`: GitHub connector updates whole files, and full rewrites have caused accidental regressions before. Prefer service/test changes unless a `GameEngine` change is necessary.

## Files to read first

```text
docs/ENGINE_HANDOFF.md
docs/ENGINE_REMAINING_WORK_PLAN.md
docs/CARD_IMPLEMENTATION_AUDIT.md
docs/MIMIC_POLICY_NOTES.md
docs/OMNIVORE_IMPLEMENTATION_NOTES.md
src/KingOfTokyo.Core/Engine/GameEngine.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
src/KingOfTokyo.Core/Domain/State/GameState.cs
src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
src/KingOfTokyo.Core/Services/KeepCardLifecycleService.cs
src/KingOfTokyo.Core/Services/MimicService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
src/KingOfTokyo.Core/Services/MarketPurchaseService.cs
src/KingOfTokyo.Core/Services/OwnedCardTransferService.cs
src/KingOfTokyo.Core/Services/FinalizeDiceService.cs
src/KingOfTokyo.Core/Rules/Attack/DamageApplier.cs
src/KingOfTokyo.Core/Rules/Attack/DamagePreventionService.cs
src/KingOfTokyo.Core/Dto/GameStateDto.cs
src/KingOfTokyo.Core/Dto/GameStateDtoMapper.cs
tests/KingOfTokyo.Core.Tests
```

## Current architecture snapshot

The project is a headless C# engine. The current shape is:

- `GameEngine` dispatches commands and coordinates services.
- `GameState` owns public game state, versioning, event log, current turn, Tokyo state, market, pending decisions, scheduled turns, and winner info.
- DTO projection exists via `GameStateDto` and `GameStateDtoMapper` for future online UI/server use.
- Most card logic is implemented through a mix of:
  - `KeepCardRulesService`,
  - `KeepCardEffectLookupService`,
  - specialized services,
  - domain state helpers,
  - command handlers,
  - integration tests.
- The codebase intentionally prioritizes stable headless rules before any UI.

## Latest known status

As of this handoff:

- The engine is in late stabilization.
- Most headless game logic and most cards are implemented.
- 2-player games are supported as a deliberate small rule change.
- EventLog + Versioning are implemented and covered.
- DTO mapper is stabilized and covered.
- Bay/Tokyo edge cases have broad regression coverage.
- Full-flow/API-readiness tests have broad regression coverage.
- Healing Ray has been moved into the main `GameEngine.Execute(IGameCommand)` command switch.
- Keep-card lifecycle cleanup is mostly complete for current production paths.
- Combined card edge-case regression coverage has started, mainly around Acid Attack.

Important: after the most recent Mimic -> Acid Attack fix, the user still needs to run the full test suite and report the result:

```bash
git pull
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

The most recent relevant commit before this handoff update was:

```text
2667a4b1c4a6fc278a6a64cdce733f7c94841b29
```

It fixed `DamageKind.DiceAttack` to `DamageKind.Attack` in `MimicAcidAttackFlowTests`.

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

### Victory and elimination timing

Expanded coverage includes:

- Current player with 20 VP must survive the turn.
- If current player reaches 20 VP but dies, they do not win.
- If no monsters remain alive, game ends with no winner.
- Non-current alive player can win with 20 VP.
- Dead player with 20 VP cannot win.
- VictoryMode.Standard, FirstToTwentyPoints, and LastMonsterStanding have targeted resolver tests.
- Eater of the Dead can trigger a non-current 20 VP win.
- It Has a Child + Eater victory timing coverage.
- Full-game regression covers 20 VP victory, last-monster-standing victory, and dead 20 VP non-victory.

Still useful later:

- Multiple players reach 20 VP in same resolution step.
- Current player and non-current player reach 20 VP through different events in same turn.
- All monsters eliminated while one or more players reached 20 VP earlier in same turn.
- LastMonsterStanding mode with all eliminated should still produce no winner.

## Important current systems/cards

### Mimic

Status: implemented for the current supported policy; stateful/self-discard copy activation remains intentionally blocked.

Implemented pieces:

- `KnownCardIds.Mimic` and default deck entry exist.
- `MarketCardState` supports `MimicTargetState`.
- DTO mapping exposes Mimic target state.
- Retarget command/service/tests exist.
- `MimicService` enforces target policy:
  - actor must own Mimic,
  - Mimic owner must be alive,
  - target owner must be alive,
  - target owner must be another player,
  - target card must exist on the target owner,
  - target card must be a keep card,
  - Mimic cannot copy another Mimic,
  - Mimic cannot copy its owner's own cards.
- Retarget cost is charged only after the new target validates and is successfully applied.
- `KeepCardEffectLookupService` can check effects directly or through Mimic.
- `MimicTargetCleanupService` clears stale targets by lost card or by owner.
- Validated Mimic lookup rejects copied effects when the original owner lost the card or is no longer valid.

Supported copied effects include:

- passive keep effects through `KeepCardRulesService`,
- Acid Attack,
- Omnivore,
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray.

Intentionally unsupported Mimic activations for now:

- Smoke Cloud,
- Plot Twist,
- Metamorph,
- Psychic Probe.

Reason: these effects use card-local counters, once-per-turn state, self-discard, or special self semantics. Tests explicitly lock the current behavior so they do not become partially active by accident.

Stale-target cleanup is covered for:

- Parasitic Tentacles transfer,
- Metamorph discard,
- Plot Twist self-discard,
- Smoke Cloud self-discard,
- Monster Batteries payment discard,
- Monster Batteries end-turn drain discard,
- Psychic Probe self-discard,
- player elimination,
- It Has a Child replacement after defeat.

Remaining architectural cleanup:

- Consider moving Mimic cleanup into the same lifecycle service so lost-card handling has one central public entry point.

Key files/tests:

```text
src/KingOfTokyo.Core/Engine/GameEngineMimicExtensions.cs
src/KingOfTokyo.Core/Services/MimicService.cs
src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
docs/MIMIC_POLICY_NOTES.md
tests/KingOfTokyo.Core.Tests/CardMimic
tests/KingOfTokyo.Core.Tests/Integration/Mimic*FlowTests.cs
```

### Omnivore

Status: implemented.

Implemented pieces:

- `KnownCardIds.Omnivore` exists.
- Default deck entry exists in `MarketSetupService`.
- `KeepCardRulesService.GetOmnivoreVictoryPoints(...)` returns `+2 VP` when the owner rolled at least one pair.
- `FinalizeDiceService` applies the Omnivore scoring bonus during dice finalization.
- Dice used for Omnivore are not consumed and remain usable for normal number scoring and other scoring effects.
- Multiple pairs still give only one Omnivore bonus per Omnivore effect.
- `Mimic -> Omnivore` works.
- Stacked effects work: real Omnivore plus `Mimic -> Omnivore` gives `+4 VP`.
- Omnivore works alongside Complete Destruction.

Key files/tests:

```text
src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
src/KingOfTokyo.Core/Services/FinalizeDiceService.cs
docs/OMNIVORE_IMPLEMENTATION_NOTES.md
tests/KingOfTokyo.Core.Tests/Integration/Omnivore*FlowTests.cs
tests/KingOfTokyo.Core.Tests/Integration/MimicOmnivore*FlowTests.cs
```

### Healing Ray

Status: implemented and moved into the main command dispatch.

Implemented pieces:

- Added to `KnownCardIds` and default deck.
- `HealingRayService` heals another player and transfers payment from target to healer.
- Target pays 2 energy per actual healed damage, or all remaining energy if they cannot pay enough.
- `TurnState` tracks `HealingRayHeartsSpent` so heart dice cannot be reused infinitely.
- `ActivateHealingRayCommand` exists.
- `ActivateHealingRayCommand` is now handled directly in `GameEngine.Execute(IGameCommand)`.
- Generic command dispatch regression coverage exists.
- Existing typed extension remains compatible.
- Mimic -> Healing Ray works.
- Mimic targeting a different card does not allow Healing Ray.
- Mimic target cleanup is respected when the copied Healing Ray is lost.

Key files/tests:

```text
src/KingOfTokyo.Core/Commands/ActivateHealingRayCommand.cs
src/KingOfTokyo.Core/Engine/GameEngine.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
src/KingOfTokyo.Core/Services/HealingRayService.cs
tests/KingOfTokyo.Core.Tests/Integration/HealingRayFlowTests.cs
tests/KingOfTokyo.Core.Tests/Integration/MimicHealingRayFlowTests.cs
```

### Keep-card lifecycle cleanup

Status: mostly complete for current production paths.

Implemented:

- Added `KeepCardLifecycleService` with added/lost hooks.
- `Even Bigger` max-health added/lost behavior is centralized there.
- Purchased keep cards apply added lifecycle effects through `MarketPurchaseService`.
- `Metamorph` discards use lifecycle lost effects.
- `Parasitic Tentacles` transfers apply lost effect to seller and added effect to buyer.
- `It Has a Child` discard-all applies lifecycle lost effects.
- `Smoke Cloud` and `Plot Twist` self-discards call lifecycle lost effects.
- `Monster Batteries` discard paths call lifecycle lost effects when batteries are exhausted by payment or end-turn drain.

Regression coverage added/expanded:

- `Parasitic Tentacles` transfers `Even Bigger` max-health effect from seller to buyer.
- `It Has a Child` discarding `Even Bigger` resets max health back to normal before revive to 10.
- Existing `Even Bigger` purchase and `Metamorph` coverage should now flow through lifecycle service.

Still useful later:

- Consider moving Mimic cleanup into the same lifecycle service so lost-card handling has one central public entry point.
- Add dedicated unit tests for `KeepCardLifecycleService` if more cards gain lifecycle hooks.
- Audit any future direct `RemoveKeepCard(...)` calls and route them through lifecycle helpers.

Key files/tests:

```text
src/KingOfTokyo.Core/Services/KeepCardLifecycleService.cs
src/KingOfTokyo.Core/Services/MarketPurchaseService.cs
src/KingOfTokyo.Core/Services/OwnedCardTransferService.cs
src/KingOfTokyo.Core/Services/SpecialCardActivationService.cs
src/KingOfTokyo.Core/Services/EnergyPaymentService.cs
src/KingOfTokyo.Core/Services/TurnLifecycleService.cs
src/KingOfTokyo.Core/Rules/Victory/EliminationService.cs
tests/KingOfTokyo.Core.Tests/Integration/ParasiticTentaclesFlowTests.cs
tests/KingOfTokyo.Core.Tests/Integration/AttackFlowTests.cs
```

### Opportunist

Status: implemented.

- Reacts when a new market card is revealed after a purchase or refresh.
- Creates `OpportunistPurchase` pending decision.
- Eligible Opportunist player can decline or buy the revealed card out of turn.
- DTO test covers the reveal decision payload.

Key files/tests:

```text
src/KingOfTokyo.Core/Commands/BuyOpportunistRevealedCardCommand.cs
src/KingOfTokyo.Core/Commands/DeclineOpportunistRevealedCardCommand.cs
tests/KingOfTokyo.Core.Tests/Integration/OpportunistReactionWindowTests.cs
```

### Parasitic Tentacles

Status: implemented.

- Added to `KnownCardIds` and default deck.
- Current player with this keep card can buy keep cards from another living player during purchase phase.
- Card transfers to buyer.
- Buyer pays seller the card cost in energy.
- Mimic target cleanup runs when the copied card leaves the seller.
- Keep-card lifecycle effects move correctly during transfer, including `Even Bigger`.
- Covered by service and GameEngine flow tests.

Key files/tests:

```text
src/KingOfTokyo.Core/Commands/BuyOwnedKeepCardCommand.cs
src/KingOfTokyo.Core/Services/OwnedCardTransferService.cs
tests/KingOfTokyo.Core.Tests/Services/OwnedCardTransferServiceTests.cs
tests/KingOfTokyo.Core.Tests/Integration/ParasiticTentaclesFlowTests.cs
```

### Acid Attack combined-card coverage

Status: actively being expanded.

Already covered:

- Acid Attack adds damage to normal dice attack.
- Acid Attack adds damage to Poison Quills card-effect damage.
- Acid Attack adds damage to bought discard-card damage against other monsters.
- Acid Attack does not add to self-damage.
- Mimic -> Acid Attack works for Fire Blast discard damage.
- Mimic -> Acid Attack works for dice attack.
- Mimic -> Acid Attack works for Poison Quills.
- Acid Attack + Gas Refinery regression coverage was added.
- Acid Attack + High Altitude Bombing regression coverage was added.

Important latest test file:

```text
tests/KingOfTokyo.Core.Tests/Integration/MimicAcidAttackFlowTests.cs
```

Recent build issue fixed there:

```text
DamageKind.DiceAttack -> DamageKind.Attack
```

Still useful next:

- Acid Attack + Armor Plating.
- Acid Attack + Camouflage.
- Acid Attack + Armor Plating + Camouflage together.
- Acid Attack with Fire Blast eliminations and Eater of the Dead.
- Confirm whether `AcidAttackPreventionFlowTests.cs` exists. A test file for prevention was planned, but if it is not present in the repo, create it fresh under the correct `king-of-tokyo-engine` path.

## Other implemented cards/systems worth knowing

These are implemented and have coverage in existing tests:

- Wings.
- Camouflage.
- Smoke Cloud.
- Monster Batteries.
- Freeze Time.
- Frenzy.
- Psychic Probe.
- Background Dweller.
- Made in a Lab.
- Rapid Healing.
- Telepath.
- Stretchy.
- Herd Culler.
- Plot Twist.
- Metamorph.
- It Has a Child.
- Eater of the Dead.
- Even Bigger.
- Burrowing / Urbavore / Spiked Tail and other attack modifiers have some coverage, but Tokyo attack modifier combinations still need more edge-case tests.

## Recommended next step

Start here after running the full test suite:

```text
Continue combined card edge-case regression coverage.
```

Suggested immediate next test block:

```text
Acid Attack + defensive/prevention cards:
- Armor Plating with Acid Attack.
- Camouflage with Acid Attack.
- Armor Plating + Camouflage with Acid Attack.
```

Implementation hint:

- Use `FinalizeDiceService` through `GameEngine`.
- For Camouflage, inject deterministic random faces through `DamagePreventionService` and `DamageApplier` the same way existing attack-flow tests do.
- Expected order is:
  1. Attack/card source determines base damage.
  2. Acid Attack adds +1 against other monsters.
  3. Armor Plating static prevention applies.
  4. Camouflage rolls against remaining incoming damage.

Then continue with:

```text
Acid Attack + Fire Blast eliminations + Eater of the Dead.
Tokyo attack modifiers: Burrowing / Urbavore / Spiked Tail stacking.
Fire Breathing neighbor targeting in 2, 3, 4, 5, and 6 player games, including Bay occupants.
```

## Current rough completion estimate

The headless engine is roughly 93-95% complete for starting an online UI/server layer, assuming the current full test suite is green.

Remaining work before UI is mostly:

- more combined-card regression coverage,
- a few explicit policy edge cases,
- final documentation/audit cleanup,
- optional event cursor / online sync helper once the server API shape is known.
