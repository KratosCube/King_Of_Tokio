using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;

namespace KingOfTokyo.Api.Contracts;

public sealed record CreateGameRequest(IReadOnlyList<string> MonsterNames);

public sealed record ActorRequest(int? ActorPlayerId);

public sealed record RerollDiceRequest(int? ActorPlayerId, IReadOnlyList<int> DiceIndexesToReroll);

public sealed record BuyFaceUpCardRequest(int? ActorPlayerId, int SlotIndex);

public sealed record ChooseLeaveTokyoRequest(int ActorPlayerId, bool LeaveTokyo);

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
