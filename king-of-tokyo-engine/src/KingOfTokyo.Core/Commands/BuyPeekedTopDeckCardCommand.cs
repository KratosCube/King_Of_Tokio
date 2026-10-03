using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyPeekedTopDeckCardCommand : CommandBase
{
    public int StoredEnergyToDeposit { get; }

    public BuyPeekedTopDeckCardCommand(int? actorPlayerId = null, int storedEnergyToDeposit = 0) : base(actorPlayerId)
    {
        StoredEnergyToDeposit = storedEnergyToDeposit;
    }
}
