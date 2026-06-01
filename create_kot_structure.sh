#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-king-of-tokyo-engine}"

DIRS=(
  "$ROOT/src/KingOfTokyo.Core/Abstractions"
  "$ROOT/src/KingOfTokyo.Core/Commands"
  "$ROOT/src/KingOfTokyo.Core/Decisions"
  "$ROOT/src/KingOfTokyo.Core/Domain/Entities"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums"
  "$ROOT/src/KingOfTokyo.Core/Domain/State"
  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects"
  "$ROOT/src/KingOfTokyo.Core/Engine"
  "$ROOT/src/KingOfTokyo.Core/Events"
  "$ROOT/src/KingOfTokyo.Core/Rules/Attack"
  "$ROOT/src/KingOfTokyo.Core/Rules/Dice"
  "$ROOT/src/KingOfTokyo.Core/Rules/Healing"
  "$ROOT/src/KingOfTokyo.Core/Rules/Scoring"
  "$ROOT/src/KingOfTokyo.Core/Rules/Tokyo"
  "$ROOT/src/KingOfTokyo.Core/Rules/Victory"
  "$ROOT/src/KingOfTokyo.Core/Services"
  "$ROOT/src/KingOfTokyo.Core/Utils"

  "$ROOT/src/KingOfTokyo.Content/Cards/Definitions"
  "$ROOT/src/KingOfTokyo.Content/Cards/Effects"
  "$ROOT/src/KingOfTokyo.Content/Cards/Registry"
  "$ROOT/src/KingOfTokyo.Content/Cards/Runtime"
  "$ROOT/src/KingOfTokyo.Content/Config"
  "$ROOT/src/KingOfTokyo.Content/Monsters"

  "$ROOT/src/KingOfTokyo.Adapters/AI"
  "$ROOT/src/KingOfTokyo.Adapters/CLI"
  "$ROOT/src/KingOfTokyo.Adapters/Testing"

  "$ROOT/tests/KingOfTokyo.Tests.Unit/Core"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules"
  "$ROOT/tests/KingOfTokyo.Tests.Integration"

  "$ROOT/docs"
  "$ROOT/scripts"
)

FILES=(
  "$ROOT/README.md"
  "$ROOT/.gitignore"

  "$ROOT/src/KingOfTokyo.Core/Abstractions/IGameEngine.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/IGameCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/IPlayerDecisionProvider.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/IRandomSource.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/IRuleResolver.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/IGameObserver.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/CommandBase.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/GameEventBase.cs"
  "$ROOT/src/KingOfTokyo.Core/Abstractions/GameRuleResolverBase.cs"

  "$ROOT/src/KingOfTokyo.Core/Commands/InitializeGameCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/BeginTurnCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/RollDiceCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/RerollDiceCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/FinalizeDiceCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/ChooseLeaveTokyoCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/EndPurchasePhaseCommand.cs"
  "$ROOT/src/KingOfTokyo.Core/Commands/AdvanceToNextPlayerCommand.cs"

  "$ROOT/src/KingOfTokyo.Core/Decisions/PendingDecision.cs"
  "$ROOT/src/KingOfTokyo.Core/Decisions/RerollDecisionData.cs"
  "$ROOT/src/KingOfTokyo.Core/Decisions/LeaveTokyoDecisionData.cs"

  "$ROOT/src/KingOfTokyo.Core/Domain/Entities/PlayerState.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Entities/TokyoState.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Entities/TurnState.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Entities/DicePoolState.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Entities/DieState.cs"

  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/DieFace.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/TokyoSlot.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/TurnPhase.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/GameStatus.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/DamageKind.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/DecisionType.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/Enums/VictoryMode.cs"

  "$ROOT/src/KingOfTokyo.Core/Domain/State/GameState.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/State/TurnFlags.cs"

  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects/GameOptions.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects/DamagePacket.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects/HealingPacket.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects/DiceResolutionSummary.cs"
  "$ROOT/src/KingOfTokyo.Core/Domain/ValueObjects/WinnerInfo.cs"

  "$ROOT/src/KingOfTokyo.Core/Engine/GameEngine.cs"
  "$ROOT/src/KingOfTokyo.Core/Engine/CommandResult.cs"
  "$ROOT/src/KingOfTokyo.Core/Engine/TurnFlowCoordinator.cs"
  "$ROOT/src/KingOfTokyo.Core/Engine/GameStateValidator.cs"

  "$ROOT/src/KingOfTokyo.Core/Events/TurnStartedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/DiceRolledEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/DiceFinalizedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/VictoryPointsGainedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/EnergyGainedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/DamageDealtEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/PlayerHealedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/TokyoEnteredEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/TokyoLeftEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/PlayerEliminatedEvent.cs"
  "$ROOT/src/KingOfTokyo.Core/Events/GameEndedEvent.cs"

  "$ROOT/src/KingOfTokyo.Core/Rules/Attack/AttackResolver.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Attack/DamageApplier.cs"

  "$ROOT/src/KingOfTokyo.Core/Rules/Dice/DiceRollService.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Dice/DiceSummaryBuilder.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Dice/DiceSelectionValidator.cs"

  "$ROOT/src/KingOfTokyo.Core/Rules/Healing/HealingResolver.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Scoring/ScoringResolver.cs"

  "$ROOT/src/KingOfTokyo.Core/Rules/Tokyo/TokyoResolver.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Tokyo/TokyoOccupancyService.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Tokyo/TokyoLeaveDecisionHandler.cs"

  "$ROOT/src/KingOfTokyo.Core/Rules/Victory/VictoryResolver.cs"
  "$ROOT/src/KingOfTokyo.Core/Rules/Victory/EliminationService.cs"

  "$ROOT/src/KingOfTokyo.Core/Services/TurnOrderService.cs"
  "$ROOT/src/KingOfTokyo.Core/Services/AlivePlayersService.cs"
  "$ROOT/src/KingOfTokyo.Core/Services/TargetingService.cs"

  "$ROOT/src/KingOfTokyo.Content/Cards/Definitions/CardDefinition.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Definitions/KeepCardDefinition.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Definitions/DiscardCardDefinition.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Effects/ICardEffect.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Effects/IReactiveEffect.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Effects/IContinuousModifier.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Effects/IActivatedAbility.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Registry/CardDefinitionRegistry.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Registry/StarterDeckFactory.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Runtime/CardInstance.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Runtime/KeepCardInstance.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Runtime/DiscardCardInstance.cs"
  "$ROOT/src/KingOfTokyo.Content/Cards/Runtime/StatusInstance.cs"
  "$ROOT/src/KingOfTokyo.Content/Config/GameConfig.cs"
  "$ROOT/src/KingOfTokyo.Content/Monsters/MonsterDefinition.cs"

  "$ROOT/src/KingOfTokyo.Adapters/AI/BasicBotDecisionProvider.cs"
  "$ROOT/src/KingOfTokyo.Adapters/CLI/Program.cs"
  "$ROOT/src/KingOfTokyo.Adapters/Testing/FakeDecisionProvider.cs"
  "$ROOT/src/KingOfTokyo.Adapters/Testing/DeterministicRandomSource.cs"

  "$ROOT/tests/KingOfTokyo.Tests.Unit/Core/GameStateTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Core/TurnFlowTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules/ScoringResolverTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules/HealingResolverTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules/AttackResolverTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules/TokyoResolverTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Unit/Rules/VictoryResolverTests.cs"
  "$ROOT/tests/KingOfTokyo.Tests.Integration/FullTurnIntegrationTests.cs"

  "$ROOT/docs/architecture.md"
  "$ROOT/docs/turn-flow.md"
  "$ROOT/docs/rules-notes.md"
  "$ROOT/scripts/run-tests.sh"
)

for dir in "${DIRS[@]}"; do
  mkdir -p "$dir"
done

for file in "${FILES[@]}"; do
  mkdir -p "$(dirname "$file")"
  touch "$file"
done

# Minimal helpful starter content
cat > "$ROOT/README.md" <<'README'
# King of Tokyo Engine

Headless C# engine for a King of Tokyo style card/dice game.
README

cat > "$ROOT/.gitignore" <<'GITIGNORE'
bin/
obj/
.vscode/
.idea/
*.user
*.suo
*.tmp
TestResults/
GITIGNORE

cat > "$ROOT/docs/architecture.md" <<'DOC'
# Architecture

- Core = čistá herní logika
- Content = definice karet a monster
- Adapters = CLI, AI, test doubles
- Tests = unit + integration
DOC

cat > "$ROOT/docs/turn-flow.md" <<'DOC'
# Turn flow

1. Begin turn
2. Roll dice (up to 3 times)
3. Finalize dice
4. Resolve scoring / energy / healing / attack
5. Purchase phase
6. End turn
DOC

cat > "$ROOT/docs/rules-notes.md" <<'DOC'
# Rules notes

Sem si piš nejasnosti a rozhodnutí implementace.
DOC

cat > "$ROOT/scripts/run-tests.sh" <<'EOF2'
#!/usr/bin/env bash
set -euo pipefail

dotnet test
EOF2
chmod +x "$ROOT/scripts/run-tests.sh"

printf 'Hotovo. Struktura vytvořena v: %s\n' "$ROOT"
