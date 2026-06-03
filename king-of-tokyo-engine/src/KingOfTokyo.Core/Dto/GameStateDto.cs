using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Dto;

public sealed record GameStateDto(
    Guid GameId,
    long Version,
    GameStatus Status,
    int CurrentPlayerIndex,
    IReadOnlyList<int> ScheduledTurnPlayerIds,
    int? WinnerPlayerId,
    string? WinnerReason,
    IReadOnlyList<PlayerDto> Players,
    TokyoDto Tokyo,
    MarketDto Market,
    TurnDto? CurrentTurn,
    PendingDecisionDto? PendingDecision);

public sealed record PlayerDto(
    int PlayerId,
    string MonsterName,
    int Health,
    int MaxHealth,
    int VictoryPoints,
    int Energy,
    TokyoSlot TokyoSlot,
    bool IsAlive,
    PlayerStatusDto Status,
    IReadOnlyList<CardDto> KeepCards);

public sealed record PlayerStatusDto(
    int PoisonTokens,
    int ShrinkTokens);

public sealed record TokyoDto(
    int? CityOccupantId,
    int? BayOccupantId,
    bool BayEnabled,
    bool IsEmpty);

public sealed record MarketDto(
    IReadOnlyList<CardDto?> FaceUpCards,
    int DrawPileCount,
    int DiscardPileCount);

public sealed record CardDto(
    string CardId,
    string Name,
    string Description,
    int Cost,
    MarketCardType CardType,
    int Counters = 0,
    int StoredEnergy = 0,
    MimicTargetDto? MimicTarget = null);

public sealed record MimicTargetDto(
    int OwnerPlayerId,
    string CardId,
    string CardName);

public sealed record TurnDto(
    int CurrentPlayerId,
    TurnPhase Phase,
    int RollCountUsed,
    int MaxRolls,
    int DiceCount,
    bool DiceResolved,
    bool PurchasePhaseFinished,
    IReadOnlyList<DieDto> Dice,
    TurnFlagsDto Flags);

public sealed record DieDto(
    int Index,
    DieFace CurrentFace,
    bool IsLocked);

public sealed record TurnFlagsDto(
    bool AttackedWithDice,
    bool DealtDamage,
    bool EnteredTokyo,
    bool StartedTurnInTokyo,
    bool ScoredVictoryPoints,
    bool EliminatedSomeone,
    bool BoughtCard,
    bool HerdCullerUsed);

public sealed record PendingDecisionDto(
    DecisionType DecisionType,
    int PlayerId,
    object? Payload);
