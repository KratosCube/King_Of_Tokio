using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyOpportunistRevealedCardCommand : CommandBase
{
    public int StoredEnergyToDeposit { get; }

    public BuyOpportunistRevealedCardCommand(int? actorPlayerId = null, int storedEnergyToDeposit = 0) : base(actorPlayerId)
    {
        StoredEnergyToDeposit = storedEnergyToDeposit;
    }
}
