using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Api.Contracts;

public sealed record CreateGameRequest(
    IReadOnlyList<string> MonsterNames,
    int? InitialHealth = null,
    int? TargetVictoryPoints = null);

public sealed record ActorRequest(int? ActorPlayerId);

public sealed record RerollDiceRequest(int? ActorPlayerId, IReadOnlyList<int> DiceIndexesToReroll);

public sealed record BuyFaceUpCardRequest(int? ActorPlayerId, int SlotIndex);

public sealed record ChooseLeaveTokyoRequest(int ActorPlayerId, bool LeaveTokyo);

public sealed record ChangeDieFaceRequest(int? ActorPlayerId, int DieIndex, DieFace TargetFace);

public sealed record DieIndexRequest(int? ActorPlayerId, int DieIndex);

public sealed record HealingRayRequest(int? ActorPlayerId, int TargetPlayerId, int HealingAmount);

public sealed record SetMimicTargetRequest(int? ActorPlayerId, int TargetOwnerPlayerId, string TargetCardId);

public sealed record BuyOwnedKeepCardRequest(int? ActorPlayerId, int SellerPlayerId, string CardId);

public sealed record MetamorphRequest(int? ActorPlayerId, string CardIdToDiscard);

public sealed record PsychicProbeRequest(int ActorPlayerId, int TargetDieIndex);

public sealed record ApiCommandResultDto(
    bool Success,
    string? Error,
    GameStateDto GameState,
    PendingDecision? PendingDecision,
    IReadOnlyList<GameEventBase> NewEvents,
    long CurrentEventSequence)
{
    public static ApiCommandResultDto From(CommandResult result)
    {
        return new ApiCommandResultDto(
            result.Success,
            result.Error,
            result.GameState.ToDto(),
            result.PendingDecision,
            result.NewEvents,
            result.GameState.EventLog.Count);
    }
}
