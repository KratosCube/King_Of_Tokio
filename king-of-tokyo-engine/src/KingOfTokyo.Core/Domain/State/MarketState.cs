using KingOfTokyo.Core.Domain.Entities;

namespace KingOfTokyo.Core.Domain.State;

public sealed class MarketState
{
    public const int FaceUpSlotCount = 3;

    private readonly Queue<MarketCardState> _drawPile = new();
    private readonly List<MarketCardState> _discardPile = new();
    private readonly List<MarketCardState?> _faceUpCards = new();

    public IReadOnlyList<MarketCardState?> FaceUpCards => _faceUpCards;
    public IReadOnlyCollection<MarketCardState> DrawPile => _drawPile;
    public IReadOnlyList<MarketCardState> DiscardPile => _discardPile;

    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;

    public MarketState()
    {
        for (var i = 0; i < FaceUpSlotCount; i++)
        {
            _faceUpCards.Add(null);
        }
    }

    public void Initialize(IEnumerable<MarketCardState> deck)
    {
        ArgumentNullException.ThrowIfNull(deck);

        _drawPile.Clear();
        _discardPile.Clear();

        for (var i = 0; i < _faceUpCards.Count; i++)
        {
            _faceUpCards[i] = null;
        }

        foreach (var card in deck)
        {
            _drawPile.Enqueue(card);
        }

        FillEmptyFaceUpSlots();
    }

    public void FillEmptyFaceUpSlots()
    {
        for (var i = 0; i < _faceUpCards.Count; i++)
        {
            if (_faceUpCards[i] is null && _drawPile.Count > 0)
            {
                _faceUpCards[i] = _drawPile.Dequeue();
            }
        }
    }

    public MarketCardState RemoveFaceUpCardAt(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);

        var card = _faceUpCards[slotIndex];
        if (card is null)
        {
            throw new InvalidOperationException("Selected market slot is empty.");
        }

        _faceUpCards[slotIndex] = null;
        FillEmptyFaceUpSlots();

        return card;
    }

    public MarketCardState PeekTopDrawCard()
    {
        if (_drawPile.Count == 0)
        {
            throw new InvalidOperationException("Market draw pile is empty.");
        }

        return _drawPile.Peek();
    }

    public MarketCardState RemoveTopDrawCard()
    {
        if (_drawPile.Count == 0)
        {
            throw new InvalidOperationException("Market draw pile is empty.");
        }

        return _drawPile.Dequeue();
    }

    public void Discard(MarketCardState card)
    {
        ArgumentNullException.ThrowIfNull(card);
        _discardPile.Add(card);
    }

    public IReadOnlyList<MarketCardState> RefreshAllFaceUpCards()
    {
        var refreshedCards = new List<MarketCardState>();

        for (var i = 0; i < _faceUpCards.Count; i++)
        {
            if (_faceUpCards[i] is not null)
            {
                refreshedCards.Add(_faceUpCards[i]!);
                _discardPile.Add(_faceUpCards[i]!);
                _faceUpCards[i] = null;
            }
        }

        FillEmptyFaceUpSlots();

        return refreshedCards;
    }

    private void ValidateSlotIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _faceUpCards.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Invalid market slot index.");
        }
    }
}