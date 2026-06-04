# Mimic policy notes

Mimic is implemented for the v1 headless engine policy.

## Final v1 policy

Mimic can target another living monster's non-Mimic keep card and can copy supported passive or explicitly supported activated effects.

For v1, stateful/self-discard Mimic activations remain intentionally blocked. This is a final v1 rule-policy decision, not an implementation gap.

Blocked v1 activations through Mimic:

- Smoke Cloud
- Plot Twist
- Metamorph
- Psychic Probe

These cards can still be visible as Mimic target state when relevant, but command activation through Mimic must fail unless the player owns the real card.

Reason: these cards use card-local counters, once-per-turn state, self-discard behavior, or special ownership semantics. Copying them safely would require explicit copy-state rules and UI explanations that are not needed for the v1 online engine.

## Targeting rules

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

A failed retarget must not spend energy and must not modify the previous target.

Regression coverage exists for failed retarget attempts against:

- the owner's own card,
- a missing card,
- another Mimic,
- a card owned by a dead player.

## Supported copied effects

Current copied-effect support includes passive and selected activated keep effects through existing services and command flows, including:

- passive keep-card rules handled by `KeepCardRulesService`,
- Acid Attack,
- Omnivore,
- Telepath,
- Stretchy,
- Herd Culler,
- Wings,
- Made in a Lab,
- Rapid Healing,
- Healing Ray.

## V1 blocked copied activations

These stateful/self-discard effects are intentionally not activatable through Mimic in v1:

- Smoke Cloud,
- Plot Twist,
- Metamorph,
- Psychic Probe.

Locked by regression tests:

```text
tests/KingOfTokyo.Core.Tests/Integration/MimicUnsupportedStatefulCardFlowTests.cs
```

## Stale target cleanup

`MimicTargetCleanupService` clears stale Mimic targets when copied cards or owners become invalid.

Currently covered cleanup paths include:

- copied card transferred by Parasitic Tentacles,
- copied card discarded by Metamorph,
- copied Plot Twist self-discard,
- copied Smoke Cloud self-discard,
- copied Monster Batteries discarded after payment,
- copied Monster Batteries discarded after end-turn drain,
- copied Psychic Probe self-discard,
- target owner eliminated,
- target owner defeated but revived by It Has a Child.

## Remaining cleanup risk

No known Mimic-specific stale-target cleanup gap remains.

The remaining structural risk is that cleanup calls are still scattered across the engine and services instead of flowing through one generic owned-card lifecycle hook.

## Future cleanup direction

Long-term, scattered cleanup calls should be replaced by a generic owned-card lifecycle hook, for example:

- `OnKeepCardLost`,
- `OnKeepCardDiscarded`,
- `OnKeepCardTransferred`.

That would centralize Mimic cleanup, Even Bigger loss effects, and any future card-loss rules.
