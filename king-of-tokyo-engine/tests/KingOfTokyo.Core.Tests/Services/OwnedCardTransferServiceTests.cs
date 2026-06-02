using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Services;

public sealed class OwnedCardTransferServiceTests
{
    [Fact]
    public void BuyKeepCardFromPlayer_Should_TransferCardAndPaySeller()
    {
        var buyer = new PlayerState(0, "Buyer");
        var seller = new PlayerState(1, "Seller");
        var card = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        seller.AddKeepCard(card);
        buyer.GainEnergy(5);

        var service = new OwnedCardTransferService();

        service.BuyKeepCardFromPlayer(buyer, seller, card.CardId, card.Cost);

        Assert.Equal(0, buyer.Energy);
        Assert.Equal(5, seller.Energy);
        Assert.Contains(buyer.KeepCards, ownedCard => ownedCard.CardId == card.CardId);
        Assert.DoesNotContain(seller.KeepCards, ownedCard => ownedCard.CardId == card.CardId);
    }

    [Fact]
    public void BuyKeepCardFromPlayer_Should_Throw_WhenBuyerAndSellerAreSamePlayer()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5));
        player.GainEnergy(5);

        var service = new OwnedCardTransferService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.BuyKeepCardFromPlayer(player, player, KnownCardIds.GiantBrain, 5));
        Assert.Equal("A player cannot buy a card from themselves.", exception.Message);
    }

    [Fact]
    public void BuyKeepCardFromPlayer_Should_Throw_WhenBuyerCannotPay()
    {
        var buyer = new PlayerState(0, "Buyer");
        var seller = new PlayerState(1, "Seller");
        var card = CreateKeepCard(KnownCardIds.GiantBrain, "Giant Brain", 5);
        seller.AddKeepCard(card);
        buyer.GainEnergy(4);

        var service = new OwnedCardTransferService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.BuyKeepCardFromPlayer(buyer, seller, card.CardId, card.Cost));
        Assert.Equal("Buyer does not have enough energy.", exception.Message);
        Assert.Equal(4, buyer.Energy);
        Assert.Equal(0, seller.Energy);
        Assert.Contains(seller.KeepCards, ownedCard => ownedCard.CardId == card.CardId);
        Assert.DoesNotContain(buyer.KeepCards, ownedCard => ownedCard.CardId == card.CardId);
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
    }
}
