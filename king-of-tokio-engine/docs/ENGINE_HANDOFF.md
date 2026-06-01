# King of Tokyo Engine — handoff dokumentace

Tento dokument shrnuje aktuální stav headless enginu, aby na práci mohl navázat další vývojář nebo AI bez nutnosti číst celou historii konverzace.

Aktuální ověřený stav při posledním běhu:

```txt
137 tests total
137 succeeded
0 failed
Build succeeded
```

Projekt je zaměřený nejdřív na čistou herní logiku bez UI. Cílem je mít deterministický, testovatelný engine, který půjde později napojit na webovou online hru, ideálně přes ASP.NET Core + SignalR/Blazor nebo jiné webové UI.

---

## Jak projekt spustit

Z adresáře `king-of-tokyo-engine`:

```bash
dotnet test KingOfTokyo.Engine.slnx
```

Pokud se objeví divné compile chyby po pullu, doporučený clean postup:

```bash
dotnet clean KingOfTokyo.Engine.slnx
dotnet build-server shutdown
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
dotnet test KingOfTokyo.Engine.slnx
```

---

## Architektura enginu

### Základní princip

Engine je headless. UI se ho má ptát pomocí commandů a zpět dostávat:

- aktualizovaný `GameState`,
- `CommandResult`,
- nově vzniklé eventy,
- případný `PendingDecision`.

Commandy jsou hlavní vstupní bod. Stav se nemá měnit přímo z UI.

### Hlavní části

```txt
src/KingOfTokyo.Core/Engine
- GameEngine
- GameStateValidator
- CommandResult
- EngineStepResult
```

```txt
src/KingOfTokyo.Core/Domain
- GameState
- PlayerState
- TurnState
- MarketState
- TokyoState
- MarketCardState
- DieState / DicePoolState
```

```txt
src/KingOfTokyo.Core/Services
- DiceRollService
- FinalizeDiceService
- MarketSetupService
- MarketPurchaseService
- MarketRefreshService
- TurnLifecycleService
- TokyoDecisionService
- RapidHealingService
- SpecialCardActivationService
- KeepCardRulesService
```

```txt
src/KingOfTokyo.Core/Rules
- Attack / DamageApplier
- Dice scoring
- Tokyo resolver
- Victory / elimination
```

```txt
src/KingOfTokyo.Core/Dto
- GameStateDto
- GameStateDtoMapper
```

DTO vrstva slouží jako budoucí bezpečný snapshot pro API / SignalR / UI. UI by nemělo přímo serializovat mutable doménový `GameState`.

---

## Aktuálně implementovaná core logika

Implementováno nebo z velké části implementováno:

- inicializace hry,
- vytvoření hráčů,
- základní tah hráče,
- rolling fáze,
- rerolly,
- finalizace kostek,
- scoring 1/2/3,
- energy z kostek,
- healing z heart kostek,
- útoky,
- Tokyo City,
- Tokyo Bay podle počtu hráčů,
- rozhodnutí obránce opustit Tokio,
- vstup do Tokia,
- VP za vstup do Tokia,
- VP za start tahu v Tokiu,
- nákup karet z marketu,
- refill marketu,
- refresh marketu,
- discard pile,
- draw pile,
- eliminace hráčů,
- vítězství podle VP nebo přežití,
- pending decision systém,
- eventy pro důležité změny,
- základní DTO snapshot hry,
- command validation pro většinu akcí.

---

## Implementované nebo rozpracované karty

Názvy `KnownCardIds` jsou autoritativní zdroj identifikátorů.

### Discard / immediate efekty

Implementované efekty zahrnují minimálně:

- Heal,
- Apartment Building,
- Corner Store,
- Energize,
- Commuter Train,
- Nuclear Power Plant,
- Drop from High Altitude,
- Fire Blast,
- High Altitude Bombing,
- Evacuation Orders,
- National Guard,
- Tanks,
- Jet Fighters,
- Gas Refinery,
- Skyscraper,
- Vast Storm.

### Keep karty a pasivní efekty

Implementované nebo zapojené:

- Giant Brain — extra reroll,
- Spiked Tail — extra attack damage,
- Alien Metabolism — discount na nákup karet,
- Gourmet — bonus VP při scoringu,
- Herbivore — VP na konci tahu, pokud hráč nedal damage,
- Regeneration — bonus healing,
- Friend of Children — bonus energy gain,
- Urbavore — bonus VP při startu v Tokiu a extra damage z Tokia,
- Rapid Healing — aktivní healing za energy,
- Solar Powered — energy na konci tahu, pokud hráč nemá žádnou,
- Energy Hoarder — VP za uloženou energy,
- Rooting for the Underdog — VP pro hráče s nejméně VP,
- Alpha Monster — VP při útoku,
- Dedicated News Team — VP při nákupu karty,
- Eater of the Dead — VP při eliminaci monstra,
- Complete Destruction — bonus VP za kombinaci 1,2,3,
- Poison Quills — damage při skórování jedniček,
- Acid Attack — extra attack damage,
- Poison Spit — poison tokeny při útoku,
- Shrink Ray — shrink tokeny při útoku,
- Nova Breath — útok poškodí všechny ostatní,
- Burrowing — extra damage do Tokia a damage novému occupantovi při odchodu z Tokia,
- Jets — healing při opuštění Tokia po damage,
- Armor Plating — ignoruje 1 damage,
- Even Bigger — zvýšení max health a loss efekt při zahození,
- We're Only Making It Stronger — VP při utržení 2+ damage,
- Herd Culler — jednou za tah změna kostky na 1,
- Made in a Lab — peek top deck card a možnost koupit,
- Metamorph — zahození vlastní keep karty za energy podle costu,
- Plot Twist — změna kostky na libovolný výsledek, karta se zahodí,
- Extra Head — extra kostka,
- Telepath — spend 1 energy pro extra reroll,
- Stretchy — spend 2 energy pro změnu kostky.

---

## Důležité design poznámky

### PendingDecision

Některé akce nejsou okamžité, ale vytváří `PendingDecision`. UI podle něj musí poznat, kdo má rozhodnout a jaký typ rozhodnutí se čeká.

Použité typy zahrnují například:

- `SelectDiceToReroll`,
- `LeaveTokyo`,
- `PeekTopDeckCardPurchase`.

### Damage pipeline

Damage by měl pokud možno procházet přes `DamageApplier`, protože tam se aplikují efekty jako:

- Armor Plating,
- We're Only Making It Stronger,
- eventuálně budoucí prevention/mitigation efekty.

U složitějších budoucích karet bude potřeba damage pipeline rozšířit o prevention okno.

### Keep card lifecycle

`PlayerState` už podporuje:

- `AddKeepCard`,
- `RemoveKeepCard`,
- `HasKeepCard`,
- `IncreaseMaxHealth`,
- `DecreaseMaxHealth`.

Důležité pro karty jako Metamorph, Plot Twist, Mimic, Monster Batteries, Smoke Cloud.

### DTO snapshot

`GameStateDtoMapper` mapuje doménový stav do read-only DTO. Další doporučený krok je přidat testy pro tento mapper, aby se DTO kontrakt nerozbil při budoucím UI.

---

## Co ještě zbývá pro logiku bez UI

Odhad aktuálního stavu:

```txt
Logika hry bez UI: cca 75–83 %
Core pravidla bez všech karet: cca 92–94 %
Karty podle počtu: cca 80–82 %
Karty podle složitosti: cca 67–71 %
Online-ready infrastruktura: cca 42–52 %
UI: 0 %
```

Zbývá přibližně 17–25 % headless logiky.

### Priorita 1 — stabilizace infrastruktury

1. Přidat testy pro `GameStateDtoMapper`.
2. Přidat nebo zkontrolovat event log a versioning v `GameState`.
3. Přidat delší end-to-end flow testy celé hry.
4. Aktualizovat audit karet podle skutečného stavu.

### Priorita 2 — složité karty s damage prevention

Tyhle karty pravděpodobně vyžadují rozšíření damage pipeline:

- Wings — reakce na damage, zaplacení energy, zrušení damage.
- Camouflage — při damage se hází a část damage se může ignorovat.

Doporučený design:

- damage nejdřív vytvoří damage context,
- engine vytvoří případný pending prevention decision,
- až po vyřešení prevention se damage skutečně aplikuje.

### Priorita 3 — karty s countery / uloženými zdroji

- Smoke Cloud — má tokeny/countery, utrácí se za extra rerolly, po vyčerpání se zahodí.
- Monster Batteries — energy uložená na kartě, později se čerpá, po vyčerpání karta mizí.

Doporučený design:

- `MarketCardState` nebo nový `OwnedCardState` by měl umět držet stav konkrétní vlastněné karty.
- Samotná definice karty a vlastněná instance karty by se ideálně měly časem oddělit.

### Priorita 4 — extra turns / turn modifiers

- Freeze Time — při splnění podmínky dává extra turn s omezením kostek.

Doporučený design:

- přidat frontu extra tahů nebo `TurnModifier`,
- testovat hlavně pořadí hráčů a návrat do normálního turn orderu.

### Priorita 5 — out-of-turn interakce

- Opportunist — nákup právě odhalené karty mimo vlastní tah.
- Psychic Probe — zásah do hodu jiného hráče.
- Healing Ray — léčení ostatních hráčů pomocí heart výsledků.
- Parasitic Tentacles — nákup keep karet od ostatních hráčů.
- Mimic — kopírování jiné keep karty.

Doporučený design:

- formalizovat reaction window systém,
- `PendingDecision` musí umět být pro jiného hráče než aktuální hráč,
- commandy musí ověřovat actor player id.

### Priorita 6 — seating order / sousedi

- Fire Breathing — damage sousedům zasaženého hráče.

Potřebuje jasný model seating orderu a alive players.

---

## Doporučený další krok

Nejbezpečnější další úkol:

```txt
Přidat testy pro GameStateDtoMapper.
```

Důvod:

- právě jsme na DTO vrstvě narazili na compile problémy,
- testy zabrání opakování,
- bude to důležité pro Blazor/SignalR UI.

Potom bych pokračoval:

1. aktualizovat audit karet,
2. přidat event/version testy,
3. začít Wings nebo Smoke Cloud podle toho, jestli chceš nejdřív damage-prevention systém nebo card counters.
