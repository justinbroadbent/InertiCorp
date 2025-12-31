namespace InertiCorp.Core.Cards;

/// <summary>
/// A deck of playable cards with draw pile and discard pile.
/// Immutable - all operations return new instances.
/// </summary>
public sealed record CardDeck
{
    private readonly IReadOnlyList<PlayableCard> _drawPile;
    private readonly IReadOnlyList<PlayableCard> _discardPile;

    public CardDeck(IReadOnlyList<PlayableCard> drawPile, IReadOnlyList<PlayableCard>? discardPile = null)
    {
        _drawPile = drawPile;
        _discardPile = discardPile ?? Array.Empty<PlayableCard>();
    }

    /// <summary>
    /// Cards remaining in draw pile.
    /// </summary>
    public int DrawPileCount => _drawPile.Count;

    /// <summary>
    /// Cards in discard pile.
    /// </summary>
    public int DiscardPileCount => _discardPile.Count;

    /// <summary>
    /// Total cards in deck (draw + discard).
    /// </summary>
    public int TotalCards => DrawPileCount + DiscardPileCount;

    /// <summary>
    /// Whether the draw pile is empty.
    /// </summary>
    public bool IsDrawPileEmpty => DrawPileCount == 0;

    /// <summary>
    /// Creates a new deck from a list of cards, shuffled with the given RNG.
    /// </summary>
    public static CardDeck Create(IReadOnlyList<PlayableCard> cards, IRng rng)
    {
        var shuffled = cards.ToList();
        rng.Shuffle(shuffled);
        return new CardDeck(shuffled);
    }

    /// <summary>
    /// Draws a card from the draw pile. If empty, reshuffles discard pile first.
    /// Returns (new deck state, drawn card).
    /// </summary>
    public (CardDeck NewDeck, PlayableCard Card) Draw(IRng rng)
    {
        if (IsDrawPileEmpty && DiscardPileCount == 0)
        {
            throw new InvalidOperationException("No cards to draw");
        }

        // Reshuffle discard pile if draw pile is empty
        var deck = this;
        if (IsDrawPileEmpty)
        {
            deck = deck.Reshuffle(rng);
        }

        var card = deck._drawPile[0];
        var newDrawPile = deck._drawPile.Skip(1).ToList();
        return (new CardDeck(newDrawPile, deck._discardPile), card);
    }

    /// <summary>
    /// Draws multiple cards from the draw pile.
    /// </summary>
    public (CardDeck NewDeck, IReadOnlyList<PlayableCard> Cards) DrawMultiple(int count, IRng rng)
    {
        var cards = new List<PlayableCard>();
        var deck = this;

        for (int i = 0; i < count && deck.TotalCards > 0; i++)
        {
            var (newDeck, card) = deck.Draw(rng);
            cards.Add(card);
            deck = newDeck;
        }

        return (deck, cards);
    }

    /// <summary>
    /// Discards a card (adds to discard pile).
    /// </summary>
    public CardDeck Discard(PlayableCard card)
    {
        return new CardDeck(_drawPile, _discardPile.Append(card).ToList());
    }

    /// <summary>
    /// Discards multiple cards.
    /// </summary>
    public CardDeck DiscardMultiple(IEnumerable<PlayableCard> cards)
    {
        return new CardDeck(_drawPile, _discardPile.Concat(cards).ToList());
    }

    /// <summary>
    /// Reshuffles the discard pile into the draw pile.
    /// </summary>
    public CardDeck Reshuffle(IRng rng)
    {
        var combined = _drawPile.Concat(_discardPile).ToList();
        rng.Shuffle(combined);
        return new CardDeck(combined, Array.Empty<PlayableCard>());
    }
}
