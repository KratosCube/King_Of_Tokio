# Mimic policy notes

Mimic is substantially implemented, but still intentionally treated as a policy-sensitive card because it can copy many different keep-card effects.

## Current implemented policy

Mimic can target another living monster's non-Mimic keep card.

The current targeting rules are enforced by `MimicService.SetTarget(...)` and covered by unit/integration tests:

- the acting player must own Mimic,
- the Mimic owner must be alive,
- the target owner must be alive,
- the target owner must be another player,
- the target card must exist in the target owner's keep cards,
- the target card must be a keep card,
- Mimic cannot copy another Mimic,
- Mimic cannot copy its owner's own cards.

## Retarget timing and cost

Initial target:

- can be set during the Mimic owner's purchase phase,
- costs 0 energy.

Retarget:

- can be changed only at the start of the Mimic owner's turn before rolling dice,
- costs 1 energy,
- the cost is charged only after the new target is validated and successfully applied.

This order is important. A failed retarget must not spend energy and must not modify the previous target.

Regression coverage exists for failed retarget attempts against:

- the owner's own card,
- a missing card,
- another Mimic,
- a card owned by a dead player.

## Supported copied effects

Current copied-effect support includes passive and selected activated keep effects through existing services and command flows, including:

- passive keep-card rules handled by `KeepCardRulesService`,
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray,
- Omnivore.

## Intentionally unsupported copied effects

These stateful/self-discard effects are intentionally not activatable through Mimic yet:

- Smoke Cloud,
- Plot Twist,
- Metamorph,
- Psychic Probe.

They can still exist as visible target state when relevant, but command activation through Mimic is blocked by tests.

Reason: these cards use card-local counters, once-per-turn markers, self-discard behavior, or special ownership semantics that need explicit copy rules before they can be safely activated through Mimic.

## Stale target cleanup

`MimicTargetCleanupService` clears stale Mimic targets when copied cards or owners become invalid.

Currently covered cleanup paths include:

- copied card transferred by Parasitic Tentacles,
- copied card discarded by Metamorph,
- copied Plot Twist self-discard,
- copied Smoke Cloud self-discard,
- copied Monster Batteries discarded after payment,
- copied Monster Batteries discarded after end-turn drain,
- target owner eliminated,
- target owner defeated but revived by It Has a Child.

## Known remaining cleanup risk

Real Psychic Probe can discard itself from inside `GameEngine.cs` when the rerolled die is energy. Mimic target cleanup for that exact discard path is still a known follow-up.

Because `GameEngine.cs` is large and connector edits replace the full file, prefer a local patch or an especially careful targeted edit for that change.

## Future cleanup direction

Long-term, scattered cleanup calls should be replaced by a generic owned-card lifecycle hook, for example:

- `OnKeepCardLost`,
- `OnKeepCardDiscarded`,
- `OnKeepCardTransferred`.

That would centralize Mimic cleanup, Even Bigger loss effects, and any future card-loss rules.
