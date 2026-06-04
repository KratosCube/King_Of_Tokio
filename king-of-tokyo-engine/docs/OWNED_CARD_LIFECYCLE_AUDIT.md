# Owned keep-card lifecycle audit

This audit records the current owned-card removal paths in the headless engine.

Status: current production paths are safe for the v1 card set. A generic lifecycle pipeline is still a good future refactor, but it is not a blocker for starting UI/server work.

## Lifecycle services

Current services:

- `KeepCardLifecycleService`
  - applies added/lost effects that are tied to owning a keep card,
  - currently centralizes `Even Bigger` max-health add/loss behavior.
- `MimicTargetCleanupService`
  - clears Mimic targets when a copied card or copied owner becomes invalid.

## Production removal/transfer paths reviewed

| Path | File | Current handling | Status |
| --- | --- | --- | --- |
| Parasitic Tentacles transfer | `OwnedCardTransferService` | Removes from seller, adds to buyer, applies lost effect to seller, added effect to buyer, clears Mimic targets for lost seller card. | Safe |
| It Has a Child replacement | `EliminationService` | Removes all keep cards, applies lost effect for each, discards all, clears owner targets during elimination cleanup. | Safe |
| Monster Batteries payment depletion | `EnergyPaymentService` | Removes depleted battery, clears Mimic targets, applies lost effect, discards, emits discard event. | Safe |
| Monster Batteries end-turn drain depletion | `TurnLifecycleService` | Removes depleted battery, clears Mimic targets, applies lost effect, discards, emits discard event. | Safe |
| Smoke Cloud self-discard | `SpecialCardActivationService` | Removes exhausted card, clears Mimic targets, applies lost effect, discards, emits discard event. | Safe |
| Plot Twist self-discard | `SpecialCardActivationService` | Removes card, clears Mimic targets, applies lost effect, discards, emits discard event. | Safe |
| Metamorph discard | `SpecialCardActivationService` | Removes chosen card, clears Mimic targets, applies lost effect, grants energy, discards, emits events. | Safe |
| Psychic Probe self-discard | `GameEngine` | Removes card, clears Mimic targets, discards, emits discard event. It does not call `ApplyLostEffect`, but Psychic Probe has no current lost lifecycle effect in the v1 card set. | Safe for v1; centralize later |
| Market purchase of keep card | `MarketPurchaseService` | Applies added lifecycle effect before adding keep card to owner. | Safe |

## Non-production / test helper removal paths

Direct `PlayerState.RemoveKeepCard(...)` calls also appear in tests and low-level helpers. These are acceptable in tests when they intentionally set up or assert state, but production rules should keep using services that apply cleanup/lifecycle behavior.

## Current conclusion

No known v1 gameplay bug remains in owned-card removal/lifecycle handling.

The only non-uniform production path is Psychic Probe self-discard. It is safe today because Psychic Probe does not have an ownership lost effect, and Mimic target cleanup is already called. If future cards add more generic lost hooks, this path should be moved out of `GameEngine` and into a central lifecycle/discard helper.

## Recommended future refactor

Add a central owned-card lifecycle helper, for example:

- `DiscardOwnedKeepCard(gameState, player, cardId, reason)`
- `TransferOwnedKeepCard(gameState, seller, buyer, cardId)`
- `OnKeepCardLost(gameState, player, card)`
- `OnKeepCardDiscarded(gameState, player, card, reason)`
- `OnKeepCardTransferred(gameState, seller, buyer, card)`

That helper should consistently handle:

1. `PlayerState.RemoveKeepCard(...)`,
2. `KeepCardLifecycleService.ApplyLostEffect(...)`,
3. `MimicTargetCleanupService.ClearTargetsForLostCard(...)`,
4. market discard when applicable,
5. discard/transfer events when applicable.

This refactor is architectural cleanup, not required for current v1 gameplay correctness.
