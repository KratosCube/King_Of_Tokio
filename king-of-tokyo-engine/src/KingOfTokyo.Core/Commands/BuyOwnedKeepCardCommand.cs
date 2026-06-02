using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class BuyOwnedKeepCardCommand : CommandBase
{
    public int SellerPlayerId { get; }
    public string CardId { get; }

    public BuyOwnedKeepCardCommand(int sellerPlayerId, string cardId, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (sellerPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sellerPlayerId));
        }

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardId));
        }

        SellerPlayerId = sellerPlayerId;
        CardId = cardId;
    }
}
