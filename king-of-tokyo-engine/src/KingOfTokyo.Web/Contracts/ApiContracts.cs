namespace KingOfTokyo.Web.Contracts;

public sealed record CreateLobbyRequest(
    string Name,
    int MaxPlayers,
    bool IsPublic,
    string HostDisplayName,
    int InitialHealth,
    int TargetVictoryPoints,
    string? HostMonsterId = null,
    string? HostMonsterName = null,
    string? HostAvatarId = null);

public sealed record JoinLobbyRequest(
    string DisplayName,
    string? MonsterId = null,
    string? MonsterName = null,
    string? AvatarId = null);

public sealed record SetLobbyReadyRequest(Guid PlayerToken, bool IsReady);

public sealed record StartLobbyRequest(Guid PlayerToken);

public sealed record LobbyJoinResultDto(
    LobbyDto Lobby,
    Guid PlayerToken,
    int PlayerId);

public sealed record LobbyStartResultDto(
    LobbyDto Lobby,
    GameStateDto Game);

public sealed record LobbyDto(
    Guid LobbyId,
    string Name,
    int MaxPlayers,
    bool IsPublic,
    int InitialHealth,
    int TargetVictoryPoints,
    string Status,
    Guid? GameId,
    IReadOnlyList<LobbySeatDto> Seats);

public sealed record LobbySeatDto(
    int PlayerId,
    string DisplayName,
    bool IsHost,
    bool IsReady,
    Guid PlayerToken,
    string MonsterId,
    string MonsterName,
    string AvatarId);

public sealed record GameStateDto(
    Guid GameId,
    long Version,
    string Status,
    int CurrentPlayerIndex,
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
    string TokyoSlot,
    bool IsAlive,
    PlayerStatusDto Status,
    IReadOnlyList<CardDto> KeepCards);

public sealed record PlayerStatusDto(int PoisonTokens, int ShrinkTokens);

public sealed record TokyoDto(int? CityOccupantId, int? BayOccupantId, bool BayEnabled, bool IsEmpty);

public sealed record MarketDto(IReadOnlyList<CardDto?> FaceUpCards, int DrawPileCount, int DiscardPileCount);

public sealed record CardDto(
    string CardId,
    string Name,
    string Description,
    int Cost,
    string CardType,
    int Counters,
    int StoredEnergy,
    MimicTargetDto? MimicTarget);

public sealed record MimicTargetDto(int OwnerPlayerId, string CardId, string CardName);

public sealed record TurnDto(
    int CurrentPlayerId,
    string Phase,
    int RollCountUsed,
    int MaxRolls,
    int DiceCount,
    bool DiceResolved,
    bool PurchasePhaseFinished,
    IReadOnlyList<DieDto> Dice,
    TurnFlagsDto Flags);

public sealed record DieDto(int Index, string CurrentFace, bool IsLocked);

public sealed record TurnFlagsDto(
    bool AttackedWithDice,
    bool DealtDamage,
    bool EnteredTokyo,
    bool StartedTurnInTokyo,
    bool ScoredVictoryPoints,
    bool EliminatedSomeone,
    bool BoughtCard,
    bool HerdCullerUsed);

public sealed record PendingDecisionDto(string DecisionType, int PlayerId, object? Payload);

public sealed record ActorRequest(int? ActorPlayerId);

public sealed record RerollDiceRequest(int? ActorPlayerId, IReadOnlyList<int> DiceIndexesToReroll);

public sealed record BuyFaceUpCardRequest(int? ActorPlayerId, int SlotIndex);

public sealed record ChooseLeaveTokyoRequest(int ActorPlayerId, bool LeaveTokyo);

public sealed record ApiCommandResultDto(
    bool Success,
    string? Error,
    GameStateDto GameState,
    PendingDecisionDto? PendingDecision,
    IReadOnlyList<object> NewEvents,
    long CurrentEventSequence);

public sealed record GameEventCursorDto(
    Guid GameId,
    long FromEventSequenceExclusive,
    long CurrentEventSequence,
    long CurrentGameVersion,
    IReadOnlyList<GameEventEnvelopeDto> Events);

public sealed record GameEventEnvelopeDto(long EventSequence, object Event);
