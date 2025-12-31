namespace InertiCorp.Core;

/// <summary>
/// Immutable container holding event decks.
/// In the new card-based flow, only CrisisDeck is used for Crisis events.
/// BoardDeck and ProjectDeck are kept for potential future use.
/// </summary>
public sealed record DeckSet(
    EventDeck CrisisDeck,
    EventDeck BoardDeck,
    EventDeck ProjectDeck)
{
    /// <summary>
    /// Gets the crisis deck.
    /// </summary>
    public EventDeck GetCrisisDeck() => CrisisDeck;

    /// <summary>
    /// Draws a crisis card.
    /// Returns a new DeckSet with the updated deck and the drawn card.
    /// </summary>
    public (DeckSet NewDeckSet, EventCard Card) DrawCrisis(IRng rng)
    {
        var clonedDeck = CrisisDeck.Clone();
        var card = clonedDeck.DrawWithReshuffle(rng);
        return (this with { CrisisDeck = clonedDeck }, card);
    }
}
