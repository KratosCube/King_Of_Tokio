# Omnivore implementation notes

Omnivore is implemented in the headless engine.

## Current status

Implemented pieces:

- `KnownCardIds.Omnivore`
- default deck entry in `MarketSetupService`
- keep-card scoring rule in `KeepCardRulesService.GetOmnivoreVictoryPoints(...)`
- scoring integration in `FinalizeDiceService`
- direct integration tests in `OmnivoreScoringFlowTests`
- Mimic compatibility regression in `MimicOmnivoreScoringFlowTests`

## Rule behavior currently covered

Omnivore grants 2 victory points when the owner rolls at least one pair among any die face:

- attack
- energy
- heart
- one
- two
- three

The dice used to satisfy the pair condition are not consumed. They still count for normal number scoring and all other normal dice resolution steps.

## Mimic behavior

`Mimic -> Omnivore` works through the existing keep-card effect counting path.

Covered behavior:

- the Mimic owner does not need to own the real Omnivore card,
- a valid Mimic target pointing at another player's Omnivore grants the Omnivore scoring bonus,
- the scoring event reason remains `Keep card: Omnivore.`

## Tests to run

From repository root:

```bash
dotnet test king-of-tokyo-engine/KingOfTokyo.Engine.slnx
```

## Follow-up documentation task

`CARD_IMPLEMENTATION_AUDIT.md` still needs a small update to move Omnivore from missing to implemented. A full-file connector update was blocked during this handoff, so prefer a local patch or a smaller editor-based change.
