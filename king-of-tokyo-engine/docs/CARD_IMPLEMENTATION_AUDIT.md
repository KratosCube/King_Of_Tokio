# King of Tokyo engine - card implementation audit

This document tracks how far the headless engine is from supporting the full card set before building the online Blazor UI.

Source of truth for this audit:

- Current engine card ids in `KnownCardIds.cs`
- Current default market deck in `MarketSetupService.cs`
- Uploaded card reference PDF used during planning

The original card text is intentionally summarized here. The goal is implementation planning, not copying final public-facing card copy.

## Status legend

| Status | Meaning |
| --- | --- |
| Implemented | Present in the default deck and core effect appears to be supported by engine code/tests. |
| Partial | Present, but missing edge cases, loss effects, timing, UI decisions, or persistence support. |
| Missing | Not yet represented in `KnownCardIds` / default deck. |
| Needs engine concept | Requires a new generic engine mechanism before the card can be implemented cleanly. |

## Currently represented in code

The current codebase already represents these cards in `KnownCardIds` and/or `MarketSetupService`:

| Card | Status | Notes |
| --- | --- | --- |
| Giant Brain | Implemented | Extra reroll through keep-card rule. |
| Heal | Implemented | Simple discard healing. |
| Apartment Building | Implemented | Simple discard VP gain. |
| Spiked Tail | Implemented | Extra attack damage. |
| Corner Store | Implemented | Simple discard VP gain. |
| Energize | Implemented | Simple discard energy gain. |
| Alien Metabolism | Implemented | Purchase discount. |
| Commuter Train | Implemented | Simple discard VP gain. |
| Gourmet | Implemented | Bonus VP when scoring dice. |
| Herbivore | Implemented | End-turn VP when no damage dealt. |
| Regeneration | Implemented | Bonus healing. |
| Friend of Children | Implemented | Bonus energy gain. |
| Urbavore | Implemented | Tokyo start-turn VP and Tokyo damage bonus. |
| Rapid Healing | Implemented | Activated keep-card healing. |
| Solar Powered | Implemented | End-turn energy if empty. |
| Energy Hoarder | Implemented | End-turn VP from stored energy. |
| Rooting for the Underdog | Implemented | End-turn VP if tied/fewest VP. Needs rule confirmation for tie behavior. |
| Alpha Monster | Implemented | VP when attacking. |
| Dedicated News Team | Implemented | VP when buying cards. |
| Eater of the Dead | Implemented | VP when a monster is eliminated. |
| Complete Destruction | Implemented | Bonus VP when scoring includes 1, 2, and 3. |
| Poison Quills | Implemented | Damage when scoring 1s. Confirm exact scored-face requirement. |
| Burrowing | Implemented | Extra damage when attacking Tokyo and damage when leaving Tokyo. |
| Armor Plating | Implemented | Damage reduction. |
| Even Bigger | Partial | Gain effect exists. Missing clean support for losing/removing the card and applying the negative effect. |
| Nuclear Power Plant | Implemented | VP + healing. |
| Fire Blast | Implemented | Damage to all other monsters. |
| High Altitude Bombing | Implemented | Damage to everyone. |
| Evacuation Orders | Implemented | Damage to all other monsters. |
| National Guard | Implemented | VP + self damage. |
| Tanks | Implemented | VP + self damage. |
| Jet Fighters | Implemented | VP + self damage. |
| Gas Refinery | Implemented | VP + damage to all other monsters. |
| Skyscraper | Implemented | Simple discard VP gain. |
| Vast Storm | Implemented | VP + damage based on opponents' energy. |
| Made in a Lab | Implemented | Peek and optionally buy top deck card. |
| Extra Head | Implemented | Extra die. |
| Telepath | Implemented | Activated extra reroll. |
| Stretchy | Implemented | Activated die face change. |

## Missing or incomplete cards from the uploaded card reference

| Card | Status | Main engine need |
| --- | --- | --- |
| Opportunist | Missing / Needs engine concept | Reaction to newly revealed market card; out-of-turn purchase window. |
| Acid Attack | Missing | Add +1 damage each turn, including non-attack damage depending on exact timing. |
| Background Dweller | Missing | Always reroll specific face/result; needs dice modification hook. |
| Jets | Missing | Prevent damage when leaving Tokyo. |
| We're Only Making It Stronger | Missing | Gain VP when losing 2+ health. Needs damage-after hook. |
| Poison Spit | Missing / Needs engine concept | Poison tokens, end-of-turn poison damage, healing-symbol removal instead of healing. |
| Freeze Time | Missing / Needs engine concept | Extra turn with one fewer die after scoring 1s. |
| Herd Culler | Missing | Once per turn set a die to 1. Similar to Stretchy but constrained/free. |
| Monster Batteries | Missing / Needs engine concept | Store energy on card and drain 2 energy per turn, then discard. |
| It Has a Child | Missing / Needs engine concept | Death replacement: discard cards, lose energy, reset to 10 health. |
| Fire Breathing | Missing / Needs engine concept | Neighbor damage when dealing damage; needs seating/adjacency model. |
| Mimic | Missing / Needs engine concept | Copy another keep card; retarget by spending energy. |
| Drop from High Altitude | Missing | VP + take Tokyo if not already controlled. |
| Wings | Missing | Spend energy to cancel damage during a turn. Needs prevention window. |
| Metamorph | Missing / Needs engine concept | Sell/discard own keep cards for energy at end turn. Needs owned-card removal. |
| Parasitic Tentacles | Missing / Needs engine concept | Buy cards from other players. Needs ownership transfer and payment to another player. |
| Camouflage | Missing / Needs engine concept | Roll per incoming damage point to prevent damage. Needs prevention hook and random roll. |
| Smoke Cloud | Missing / Needs engine concept | Charge counter card, spend charges for extra rerolls, auto-discard. |
| Frenzy | Missing / Needs engine concept | Immediate extra turn after purchase. |
| Healing Ray | Missing / Needs engine concept | Heal other monsters using healing dice and transfer energy/payment. |
| Plot Twist | Missing | One-use die result change, then discard. Needs one-shot keep/discard command. |
| Nova Breath | Missing | Attacks damage all other monsters regardless of Tokyo position. |
| Omnivore | Missing / Needs engine concept | Special scoring with pairs; dice can still be used in other combos. |
| Shrink Ray | Missing / Needs engine concept | Shrink tokens, fewer dice, healing-symbol removal instead of healing. |
| Psychic Probe | Missing / Needs engine concept | Reroll one die during another player's turn; discard on heart result. |

## Recommended engine concepts to add next

### 1. Event log and game version

Add persistent event tracking so the online server can replay, debug, and resync clients.

Suggested fields:

```csharp
public Guid GameId { get; }
public long Version { get; private set; }
public IReadOnlyList<GameEventBase> EventLog => _eventLog;
```

### 2. Card effect pipeline

Replace the growing `KeepCardRulesService` conditional list over time with effect hooks:

```csharp
public interface ICardEffectHandler
{
    void OnStartTurn(CardEffectContext context) { }
    void OnBeforeDamage(CardEffectContext context, DamageContext damage) { }
    void OnAfterDamage(CardEffectContext context, DamageContext damage) { }
    void OnDiceFinalized(CardEffectContext context, DiceResolutionSummary summary) { }
    void OnCardRevealed(CardEffectContext context, MarketCardState card) { }
    void OnEndTurn(CardEffectContext context) { }
}
```

Keep simple discard cards as data-only `CardPurchaseEffect`. Move complex keep cards into handlers.

### 3. Player status effects

Needed for Poison Spit and Shrink Ray:

```csharp
public sealed class PlayerStatusState
{
    public int PoisonTokens { get; private set; }
    public int ShrinkTokens { get; private set; }
}
```

### 4. Owned-card lifecycle

Needed for Even Bigger loss effect, Metamorph, Mimic, Smoke Cloud, Plot Twist, Monster Batteries.

Required operations:

- Add keep card
- Remove keep card
- Discard keep card
- Transfer keep card
- Attach counters/tokens to a card
- Run `OnCardLost` / `OnCardDiscarded` effects

### 5. Out-of-turn reactions

Needed for Opportunist and Psychic Probe.

This should probably reuse `PendingDecision`, but support multiple eligible players and timeouts once online.

### 6. Server-facing DTOs

Before Blazor UI, create DTOs that hide mutable domain objects:

```csharp
public sealed record GameStateDto(
    Guid GameId,
    long Version,
    GameStatus Status,
    IReadOnlyList<PlayerDto> Players,
    TokyoDto Tokyo,
    MarketDto Market,
    TurnDto? CurrentTurn,
    PendingDecisionDto? PendingDecision);
```

## Proposed implementation order

1. Add event log + game version.
2. Add DTO projection layer.
3. Add card implementation tests that compare expected card ids against the deck.
4. Add owned-card removal/lifecycle support.
5. Add player status effects: poison and shrink.
6. Add effect pipeline while keeping current `KeepCardRulesService` as an adapter.
7. Implement missing simple cards: Acid Attack, Jets, Drop from High Altitude, Nova Breath.
8. Implement token cards: Poison Spit, Shrink Ray.
9. Implement complex reaction cards: Opportunist, Psychic Probe, Mimic.
10. Start SignalR server and Blazor client only after the headless engine can complete representative games.
