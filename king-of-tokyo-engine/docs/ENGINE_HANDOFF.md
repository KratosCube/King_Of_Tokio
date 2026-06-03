# King of Tokyo engine handoff

This document is the current handoff for continuing the headless King of Tokyo / Vládce Tokia engine before building any UI.

Repository path:

```text
king-of-tokyo-engine
```

Important: there was previously an accidental wrong folder path with `tokio`/`tokio` confusion. Continue using only:

```text
king-of-tokyo-engine
```

Primary solution command from repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

Latest user-reported status before this handoff update:

```text
Tests were green after Mimic policy/cleanup, Psychic Probe cleanup, and Omnivore regression work.
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
src/KingOfTokyo.Core/Services/MimicService.cs
src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
src/KingOfTokyo.Core/Services/MarketSetupService.cs
src/KingOfTokyo.Core/Services/FinalizeDiceService.cs
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
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray through the typed extension path,
- Omnivore.

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

No known Mimic-specific stale-target cleanup gap remains. The remaining architectural cleanup is to replace scattered cleanup calls with a generic owned-card lifecycle hook.

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
- Mimic target cleanup now runs when real Psychic Probe discards itself.

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

The main card mechanics are now implemented for the current engine policy.

Remaining card-related work is mostly cleanup/policy/refactor rather than missing card implementation:

- Keep stateful/self-discard Mimic activations blocked unless explicit copy semantics are designed.
- Add a generic owned-card lifecycle hook to centralize cleanup and loss effects.
- Move Healing Ray from typed extension into the main `GameEngine` command switch if uniform command dispatch becomes required.
- Review partial edge-case cards noted in `CARD_IMPLEMENTATION_AUDIT.md`, especially Drop from High Altitude, Even Bigger loss paths, Jets/Bay timing, and Acid Attack wording.

## Other useful follow-up work

Recommended next steps:

1. Add one longer full-game regression flow that uses several recently completed mechanics together.
2. Add more Bay/Tokyo edge-case tests.
3. Add more simultaneous elimination/victory timing tests.
4. Add generic owned-card lifecycle hooks:
   - add keep card,
   - remove keep card,
   - discard keep card,
   - transfer keep card,
   - run `OnCardLost` / `OnCardDiscarded` from every path.
5. Move `Healing Ray` from extension into main `GameEngine` command switch.
6. Consider event cursor / snapshot DTO for online sync before building the UI.

## Current approximate completion

Pure headless game logic is roughly:

```text
95-97% complete
3-5% remaining
```

Most base engine systems and most cards are done. The remaining complexity is concentrated in edge-case regression coverage, lifecycle refactor cleanup, Healing Ray command-dispatch cleanup, and preparing DTO/event behavior for online sync.

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
- king-of-tokyo-engine/docs/MIMIC_POLICY_NOTES.md
- king-of-tokyo-engine/docs/OMNIVORE_IMPLEMENTATION_NOTES.md
- src/KingOfTokyo.Core/Engine/GameEngine.cs
- src/KingOfTokyo.Core/Engine/GameEngineHealingRayExtensions.cs
- src/KingOfTokyo.Core/Domain/State/GameState.cs
- src/KingOfTokyo.Core/Domain/Entities/TurnState.cs
- src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs
- src/KingOfTokyo.Core/Domain/Entities/MarketCardState.cs
- src/KingOfTokyo.Core/Domain/ValueObjects/KnownCardIds.cs
- src/KingOfTokyo.Core/Services/KeepCardRulesService.cs
- src/KingOfTokyo.Core/Services/KeepCardEffectLookupService.cs
- src/KingOfTokyo.Core/Services/MimicService.cs
- src/KingOfTokyo.Core/Services/MimicTargetCleanupService.cs
- src/KingOfTokyo.Core/Services/MarketSetupService.cs
- src/KingOfTokyo.Core/Services/FinalizeDiceService.cs
- src/KingOfTokyo.Core/Dto/GameStateDto.cs
- src/KingOfTokyo.Core/Dto/GameStateDtoMapper.cs
- tests/KingOfTokyo.Core.Tests

Aktuální stav:
- většina headless logiky a prakticky všechny hlavní karty jsou implementované,
- EventLog + Versioning jsou hotové,
- DTO mapper je stabilizovaný,
- Opportunist, Parasitic Tentacles, Healing Ray, Mimic a Omnivore jsou implementované,
- Healing Ray je zatím napojený přes typed GameEngine extension, ne přímo v hlavním command switchi,
- Mimic má target state, DTO, retargeting, policy validace, copied efekty, validated lookup a cleanup neplatných targetů,
- žádný známý Mimic-specific stale-target cleanup gap nezbývá,
- stateful/self-discard Mimic aktivace jako Smoke Cloud, Plot Twist, Metamorph a Psychic Probe jsou zatím záměrně blokované,
- čistá herní logika je zhruba 95-97 % hotová.

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
Začni delším regression flow přes více dokončených mechanik, nebo začni návrhem generic owned-card lifecycle hooku. Postupuj po malých testovaných krocích.

Důležitý styl práce:
- Neměň moc věcí najednou, pokud to není bezpečné.
- Když přidáš produkční kód, přidej k němu test.
- Používej existující command pattern, validators, services a events.
- UI zatím neřeš, jen headless logiku.
- Neposílej obrovské diffy, spíš stručné shrnutí změn a commit.
- Odpovídej česky.
```
