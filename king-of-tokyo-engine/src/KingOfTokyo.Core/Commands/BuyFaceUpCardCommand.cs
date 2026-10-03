using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyFaceUpCardCommand : CommandBase
{
    public int SlotIndex { get; }
    public int StoredEnergyToDeposit { get; }

    public BuyFaceUpCardCommand(int slotIndex, int? actorPlayerId = null, int storedEnergyToDeposit = 0) : base(actorPlayerId)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        SlotIndex = slotIndex;
        StoredEnergyToDeposit = storedEnergyToDeposit;
    }
}
