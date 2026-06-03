# King of Tokyo engine handoff

This document is the current handoff for continuing the headless King of Tokyo / Vládce Tokia engine before building any UI.

Repository path:

```text
king-of-tokyo-engine
```

Important: there was previously an accidental wrong folder path with `tokio` instead of `tokyo`/`tokio` confusion. Continue using only:

```text
king-of-tokyo-engine
```

Primary solution command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

Latest user-reported status before this handoff update:

```text
Tests were green after the Mimic unsupported Psychic Probe test argument fix.
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
- Be careful with `GameEngine.cs`: the GitHub connector only supports full-file updates, and full rewrites have broken imports/structure before. Prefer smaller service/test changes unless a local patch is available.

## Files to read first

Read these before making new changes:

```text
docs/ENGINE_HANDOFF.md
docs/CARD_IMPLEMENTATION_AUDIT.md
src/KingOfTokyo.Core/Engine/GameEngine.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
src/KingOfTokyo.Core/Domain/State/GameState.cs
src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
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
  - specialized services,
  - domain state helpers,
  - command handlers,
  - integration tests.
- The codebase intentionally prioritizes stable headless rules before any UI.

## Important current state

### Event log and versioning

Implemented.

`GameState` tracks:

- `GameId`
- `Version`
- `EventLog`
- pending decisions
- scheduled extra turns

Tests cover successful and failed command behavior, version increments, event ordering, and DTO exposure.

### DTO layer

Implemented and stabilized.

`GameStateDtoMapper` maps:

- game id and version,
- game status and winner info,
- players,
- Tokyo state,
- market state,
- current turn,
- dice,
- turn flags,
- pending decisions,
- card counters,
- stored energy,
- Mimic target state,
- scheduled turns,
- Opportunist reveal decisions.

Relevant test area:

```text
tests/KingOfTokyo.Core.Tests/Dto
```

### Representative flow

There is a longer representative game flow test covering multiple turns, Tokyo decisions, purchases, healing, scoring, start-in-Tokyo VP, versioning, and event log consistency.

Relevant test area:

```text
tests/KingOfTokyo.Core.Tests/Integration/RepresentativeGameFlowTests.cs
```

## Recently implemented / important cards

The following cards were implemented or stabilized during the latest work sequence.

### Mimic

Status: Partial, but now substantially implemented.

Implemented pieces:

- `KnownCardIds.Mimic` and default deck entry exist.
- `MarketCardState` supports `MimicTargetState`.
- DTO mapping exposes Mimic target state.
- Retarget command/service/tests exist.
- `KeepCardEffectLookupService` can check effects directly or through Mimic.
- `MimicTargetCleanupService` can clear stale targets by lost card or by owner.
- Stale-target cleanup is covered for major card-lost paths:
  - Parasitic Tentacles transfer,
  - Metamorph discard,
  - Plot Twist self-discard,
  - Smoke Cloud self-discard,
  - Monster Batteries payment discard,
  - Monster Batteries end-turn drain discard,
  - player elimination / It Has a Child replacement.
- Validated Mimic lookup rejects copied effects when the original owner lost the card or is no longer valid.

Supported copied effects include at least:

- passive keep effects through `KeepCardRulesService`,
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray through the typed extension path.

Intentionally unsupported Mimic targets for now:

- Smoke Cloud,
- Plot Twist,
- Metamorph,
- Psychic Probe.

Reason: these effects use card-local counters, once-per-turn state, self-discard, or special self semantics. Tests explicitly lock the current behavior so they do not become partially active by accident.

Known remaining Mimic work:

- Review all copied passive effects and decide if every passive should use validated `GameState` lookup instead of simple target lookup.
- Decide whether stateful/self-discard targets remain permanently blocked or receive explicit semantics.
- Consider adding a small local patch for Psychic Probe discard cleanup in `GameEngine.cs` if the real card discards itself and Mimics point at it. Avoid full-file connector rewrites.
- Eventually replace scattered cleanup calls with a generic `OnCardLost` lifecycle hook.

Key files/tests:

```text
src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
tests/KingOfTokyo.Core.Tests/CardMimic
tests/KingOfTokyo.Core.Tests/Integration/Mimic*FlowTests.cs
```

### Wings

Implemented.

- Activation after damage taken this turn.
- Spends 2 energy.
- Cancels/heals damage taken.
- Emits `DamageCanceledEvent`.
- Preserves/updates pending Tokyo leave decisions.
- Usable through valid Mimic copy.

### Camouflage

Implemented.

- Rolls one die per remaining incoming damage after static prevention.
- Each heart prevents 1 damage.
- Covered by damage prevention and attack-flow tests.

### Smoke Cloud

Implemented.

- Starts with counters/charges.
- Spends a charge for an extra reroll.
- Discards itself when exhausted.
- Counters are exposed through DTO.
- Mimic target cleanup runs when real Smoke Cloud discards.
- Mimic activation is intentionally unsupported for now.

### Monster Batteries

Implemented.

- Starts with stored energy.
- Stored energy can pay card and refresh costs.
- Drains at end of turn.
- Discards when empty.
- Stored energy is exposed through DTO.
- Mimic target cleanup runs when real Monster Batteries are discarded by payment or end-turn drain.

### Freeze Time

Implemented.

- Schedules an extra turn after scoring three 1s.
- Extra turn uses one fewer die.
- Scheduled turns are exposed through DTO.

### Frenzy

Implemented.

- Buying Frenzy schedules an immediate extra turn.
- `CardBoughtEvent` now has both `Cost` and backward-compatible alias `CostSpent`.

### Psychic Probe

Implemented.

- Out-of-turn activation during another player's rolling phase.
- Rerolls one die.
- Discards itself if rerolled die is energy.
- Limited to once during each other player's turn.
- Mimic activation is intentionally unsupported for now.
- Potential future cleanup: when real Psychic Probe discards itself in `GameEngine.cs`, clear Mimics pointing at that specific card. Do this with a small patch, not a full-file rewrite.

### Background Dweller

Implemented.

- Automatically rerolls rolled/rerolled `Three` results until none remain for the owner during normal roll/reroll flow.

### Opportunist

Implemented.

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

Implemented.

- Added to `KnownCardIds` and default deck.
- Current player with this keep card can buy keep cards from another living player during purchase phase.
- Card transfers to buyer.
- Buyer pays seller the card cost in energy.
- Mimic target cleanup runs when the copied card leaves the seller.
- Covered by service and GameEngine flow tests.

Key files/tests:

```text
src/KingOfTokyo.Core/Commands/BuyOwnedKeepCardCommand.cs
src/KingOfTokyo.Core/Services/OwnedCardTransferService.cs
tests/KingOfTokyo.Core.Tests/Services/OwnedCardTransferServiceTests.cs
tests/KingOfTokyo.Core.Tests/Integration/ParasiticTentaclesFlowTests.cs
```

### Healing Ray

Implemented, but with one known cleanup note.

- Added to `KnownCardIds` and default deck.
- `HealingRayService` heals another player and transfers payment from target to healer.
- Target pays 2 energy per actual healed damage, or all remaining energy if they cannot pay enough.
- `TurnState` tracks `HealingRayHeartsSpent` so heart dice cannot be reused infinitely.
- `ActivateHealingRayCommand` exists as a typed command.
- There is a `GameEngineHealingRayExtensions` extension with an `Execute(this GameEngine, GameState, ActivateHealingRayCommand)` overload.
- Integration tests cover successful Healing Ray activation and failing when using more hearts than rolled.
- Valid Mimic copies can activate Healing Ray through this typed extension, and copied-card owner loss is validated.

Important cleanup note:

```text
Healing Ray is currently handled through a typed GameEngine extension instead of the main GameEngine command switch.
```

This works for typed calls such as:

```csharp
engine.Execute(gameState, new ActivateHealingRayCommand(targetId, healingAmount, actorPlayerId));
```

But a future cleanup should move it into the normal `IGameCommand` command switch if command handling needs to be fully uniform.

Key files/tests:

```text
src/KingOfTokyo.Core/Commands/ActivateHealingRayCommand.cs
src/KingOfTokyo.Core/Services/HealingRayService.cs
src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
tests/KingOfTokyo.Core.Tests/CardHealing/HealingRayServiceTests.cs
tests/KingOfTokyo.Core.Tests/Integration/HealingRayFlowTests.cs
tests/KingOfTokyo.Core.Tests/Integration/MimicHealingRayFlowTests.cs
```

## Current remaining card work

According to `docs/CARD_IMPLEMENTATION_AUDIT.md`, the main missing or incomplete card logic is now:

```text
Mimic final review / policy cleanup
Omnivore
```

### Mimic

Status: Partial, mostly implemented.

Remaining work:

- Final review of copied effects.
- Decide whether stateful/self-discard targets stay blocked.
- Add/confirm no circular Mimic copy edge cases in retargeting.
- Optional cleanup for real Psychic Probe discard path.
- Eventually replace scattered cleanup calls with generic lifecycle hooks.

Suggested next step:

1. Do a small review pass over `KeepCardRulesService` methods and `Mimic*` tests.
2. Add missing regression tests only where there is a real gap.
3. Then mark Mimic as implemented/partial-policy-complete in the audit.

### Omnivore

Status: Missing / Needs special scoring extension.

Likely required concepts:

- special scoring based on pairs,
- dice used for Omnivore should still be usable in other scoring combinations according to the audit note,
- deterministic scoring tests.

Suggested next step after Mimic review:

1. Add `KnownCardIds.Omnivore` and default deck entry if not already present.
2. Add scoring test for the exact pair condition.
3. Implement the scoring extension in dice finalization/scoring rules.

## Other useful follow-up work

After Mimic and Omnivore:

- Add a longer full-game regression flow.
- Add more Bay/Tokyo edge-case tests.
- Add more simultaneous elimination tests.
- Add generic owned-card lifecycle hooks:
  - add keep card,
  - remove keep card,
  - discard keep card,
  - transfer keep card,
  - run `OnCardLost` / `OnCardDiscarded` from every path.
- Move `Healing Ray` from extension into main `GameEngine` command switch.
- Consider event cursor / snapshot DTO for online sync before building the UI.

## Current approximate completion

Pure headless game logic is roughly:

```text
93-95% complete
5-7% remaining
```

Most base engine systems and most cards are done. The remaining complexity is concentrated in finalizing Mimic policy/cleanup, implementing Omnivore, generic lifecycle cleanup, and one longer regression flow.

## Prompt for next chat

A good continuation prompt is:

```text
Ahoj, pracuju na headless enginu pro webovou online hru inspirovanou King of Tokyo / Vládce Tokia. Chci nejdřív dokončit kompletní logiku bez UI, potom nad tím postavit online webovou hru.

Repozitář:
https://github.com/KratosCube/King_Of_Tokio/tree/main/king-of-tokyo-engine

Důležité: v projektu je aktuální handoff dokument:
king-of-tokyo-engine/docs/ENGINE_HANDOFF.md

Prosím nejdřív si projdi hlavně:
- king-of-tokyo-engine/docs/ENGINE_HANDOFF.md
- king-of-tokyo-engine/docs/CARD_IMPLEMENTATION_AUDIT.md
- src/KingOfTokyo.Core/Engine/GameEngine.cs
- src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
- src/KingOfTokyo.Core/Domain/State/GameState.cs
- src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
- src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
- src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
- src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
- src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
- src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
- src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
- src/KingOfTokyo.Core/Services/MarketSetupService.cs
- src/KingOfTokyo.Core/Dto/GameStateDto.cs
- src/KingOfTokyo.Core/Dto/GameStateDtoMapper.cs
- tests/KingOfTokyo.Core.Tests

Aktuální stav:
- většina headless logiky a většina karet je implementovaná,
- EventLog + Versioning jsou hotové,
- DTO mapper je stabilizovaný,
- Opportunist, Parasitic Tentacles a Healing Ray jsou implementované,
- Healing Ray je zatím napojený přes typed GameEngine extension, ne přímo v hlavním command switchi,
- Mimic je z velké části implementovaný: target state, DTO, retargeting, kopírované efekty, validated lookup a cleanup neplatných targetů,
- stateful/self-discard Mimic targety jako Smoke Cloud, Plot Twist, Metamorph a Psychic Probe jsou zatím záměrně blokované,
- podle auditu zbývá hlavně finalizovat Mimic policy/review a implementovat Omnivore,
- čistá herní logika je zhruba 93-95 % hotová.

Před pokračováním spusť / vyžádej si výsledek:
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx

Chci pokračovat stejně jako předchozí AI:
- navrhni další malý až střední krok,
- ideálně rovnou uprav GitHub soubory,
- po každé změně napiš, co jsi udělal,
- napiš commit SHA,
- napiš příkaz, který mám spustit lokálně,
- po mém výsledku testů pokračuj dál,
- vždy dávej pozor, aby build zůstal zelený,
- pokud něco rozbiješ, nejdřív to oprav.

Doporučený další krok:
Dokonči krátký Mimic review/policy pass a potom začni kartu Omnivore. Postupuj malými kroky: nejdřív přesný scoring test, potom card id/deck entry, potom implementace scoring extension.

Důležitý styl práce:
- Neměň moc věcí najednou, pokud to není bezpečné.
- Když přidáš produkční kód, přidej k němu test.
- Používej existující command pattern, validators, services a events.
- UI zatím neřeš, jen headless logiku.
- Neposílej obrovské diffy, spíš stručné shrnutí změn a commit.
- Odpovídej česky.
```
