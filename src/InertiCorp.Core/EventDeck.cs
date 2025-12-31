namespace InertiCorp.Core;

/// <summary>
/// A deck of event cards with draw and discard piles.
/// </summary>
public sealed class EventDeck
{
    private readonly List<EventCard> _drawPile;
    private readonly List<EventCard> _discardPile;

    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;
    public bool IsDrawPileEmpty => _drawPile.Count == 0;

    public EventDeck(IEnumerable<EventCard> cards)
    {
        _drawPile = new List<EventCard>(cards);
        _discardPile = new List<EventCard>();
    }

    /// <summary>
    /// Shuffles the draw pile using the provided RNG.
    /// </summary>
    public void Shuffle(IRng rng)
    {
        rng.Shuffle(_drawPile);
    }

    /// <summary>
    /// Returns the top card without removing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">If draw pile is empty.</exception>
    public EventCard Peek()
    {
        if (_drawPile.Count == 0)
            throw new InvalidOperationException("Cannot peek: draw pile is empty.");

        return _drawPile[0];
    }

    /// <summary>
    /// Draws the top card from the draw pile and moves it to discard.
    /// </summary>
    /// <exception cref="InvalidOperationException">If draw pile is empty.</exception>
    public EventCard Draw()
    {
        if (_drawPile.Count == 0)
            throw new InvalidOperationException("Cannot draw: draw pile is empty. Use DrawWithReshuffle instead.");

        var card = _drawPile[0];
        _drawPile.RemoveAt(0);
        _discardPile.Add(card);
        return card;
    }

    /// <summary>
    /// Draws a card, reshuffling the discard pile into draw pile if needed.
    /// </summary>
    public EventCard DrawWithReshuffle(IRng rng)
    {
        if (_drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDraw(rng);
        }

        return Draw();
    }

    /// <summary>
    /// Moves all cards from discard pile to draw pile and shuffles.
    /// </summary>
    private void ReshuffleDiscardIntoDraw(IRng rng)
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(rng);
    }

    /// <summary>
    /// Creates a deep copy of this deck with the same draw and discard piles.
    /// </summary>
    public EventDeck Clone()
    {
        var clone = new EventDeck(_drawPile);
        clone._discardPile.AddRange(_discardPile);
        clone._drawPile.Clear();
        clone._drawPile.AddRange(_drawPile);
        return clone;
    }
}
