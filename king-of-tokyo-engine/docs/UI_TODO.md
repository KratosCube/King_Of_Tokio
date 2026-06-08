# UI TODO

This document tracks UI work for the online web MVP and later polish. Keep this separate from engine TODOs so postponed UX ideas do not block MVP stabilization.

## Current MVP focus

- Keep lobby flow stable.
- Keep the game table playable in one browser session through the Blazor dev player control switch.
- Keep debug tools available, but out of the main player flow.
- Prioritize clear next-action guidance, readable monster state, dice flow, market flow, and pending decisions.

## MVP-critical UI work

- [ ] Continue tightening the main game table flow after manual smoke tests.
- [ ] Improve lobby room guidance for host, joined player, ready state, and game start.
- [ ] Make pending decisions visually clear and hard to miss.
- [ ] Keep event feed hydrated after reload/session switch.
- [ ] Review responsive layout once the desktop MVP is stable.

## Post-MVP polish ideas

### Event feed source breakdown on hover

Keep the event feed concise by default, but show a structured breakdown when hovering or focusing an event row.

Examples:

- Damage event row: `Cyber Kitty dealt 4 damage to The King.`
  - Hover/focus details:
    - Attack dice: 3
    - Acid Attack: +1
    - Total applied: 4
- Healing event row: `The King healed 3 HP.`
  - Hover/focus details:
    - Dice healing: 2
    - Regeneration: +1
    - Actual healed: 3
- Victory points or energy row:
  - Show dice source, Tokyo source, and keep/discard card bonuses separately.

Implementation notes:

- This should not block MVP.
- A pure UI-only version would be limited because current events mostly expose only a total `Amount` plus one text `Reason`.
- Proper implementation should add a small structured source model in Core/API event payloads, for example:

```csharp
public sealed record EffectSourceBreakdown(
    string SourceType,
    string Label,
    int Amount,
    string? CardId = null,
    string? CardName = null);
```

- Candidate event types for source breakdown:
  - `DamageDealtEvent`
  - `PlayerHealedEvent`
  - `EnergyGainedEvent`
  - `VictoryPointsGainedEvent`
  - optionally status-token events later
- UI can then render the current compact event text and reveal the source breakdown through hover, focus, or a small details popover.
