using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Dto;

public sealed record GameStateDto(
    Guid GameId,
    long Version,
    GameStatus Status,
    int CurrentPlayerIndex,
    IReadOnlyList<PlayerDto> Players,
    TokyoDto Tokyo,
    MarketDto Market,
    TurnDto? CurrentTurn,
    PendingDecisionDto? PendingDecision,
    WinnerInfoDto? WinnerInfo,
    int EventLogCount);

public sealed record PlayerDto(
    int PlayerId,
    string MonsterName,
    int Health,
    int MaxHealth,
    int VictoryPoints,
    int Energy,
    TokyoSlot TokyoSlot,
    PlayerStatusDto Status,
    bool IsAlive,
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

public sealed record TurnDto(
    int CurrentPlayerId,
    TurnPhase Phase,
    int RollCountUsed,
    int MaxRolls,
    int DiceCount,
    bool DiceResolved,
    bool PurchasePhaseFinished,
    TurnFlagsDto Flags,
    IReadOnlyList<DieDto> Dice);

public sealed record TurnFlagsDto(
    bool AttackedWithDice,
    bool DealtDamage,
    bool EnteredTokyo,
    bool StartedTurnInTokyo,
    bool ScoredVictoryPoints,
    bool EliminatedSomeone,
    bool BoughtCard,
    bool HerdCullerUsed);

public sealed record DieDto(
    int Index,
    DieFace CurrentFace,
    bool IsLocked);

public sealed record CardDto(
    string CardId,
    string Name,
    string Description,
    int Cost,
    MarketCardType CardType);

public sealed record PendingDecisionDto(
    DecisionType DecisionType,
    int PlayerId,
    object? Payload);

public sealed record WinnerInfoDto(
    int WinnerPlayerId,
    string Reason);
