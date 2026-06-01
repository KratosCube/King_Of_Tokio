using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Events;

public sealed class TokyoEnteredEvent : GameEventBase
{
    public int PlayerId { get; }
    public TokyoSlot Slot { get; }

    public TokyoEnteredEvent(int playerId, TokyoSlot slot)
    {
        if (slot == TokyoSlot.None)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Tokyo entry must use City or Bay.");
        }

        PlayerId = playerId;
        Slot = slot;
    }

    public override string EventName => nameof(TokyoEnteredEvent);
}