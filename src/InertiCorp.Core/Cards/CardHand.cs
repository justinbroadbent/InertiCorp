namespace InertiCorp.Core.Cards;

/// <summary>
/// The player's current hand of cards.
/// </summary>
public sealed record CardHand(IReadOnlyList<PlayableCard> Cards)
{
    public const int MaxHandSize = 7;

    /// <summary>
    /// Number of cards in hand.
    /// </summary>
    public int Count => Cards.Count;

    /// <summary>
    /// Whether the hand is full.
    /// </summary>
    public bool IsFull => Count >= MaxHandSize;

    /// <summary>
    /// Whether the hand is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Creates an empty hand.
    /// </summary>
    public static CardHand Empty => new(Array.Empty<PlayableCard>());

    /// <summary>
    /// Gets a card by index.
    /// </summary>
    public PlayableCard this[int index] => Cards[index];

    /// <summary>
    /// Checks if the hand contains a specific card.
    /// </summary>
    public bool Contains(string cardId) => Cards.Any(c => c.CardId == cardId);

    /// <summary>
    /// Returns a new hand with a card added.
    /// </summary>
    public CardHand WithCardAdded(PlayableCard card)
    {
        if (IsFull) throw new InvalidOperationException("Hand is full");
        return new CardHand(Cards.Append(card).ToList());
    }

    /// <summary>
    /// Returns a new hand with a card removed.
    /// </summary>
    public CardHand WithCardRemoved(string cardId)
    {
        var card = Cards.FirstOrDefault(c => c.CardId == cardId);
        if (card is null) throw new ArgumentException($"Card {cardId} not in hand");
        return new CardHand(Cards.Where(c => c.CardId != cardId).ToList());
    }

    /// <summary>
    /// Returns a new hand with multiple cards added (up to max hand size).
    /// </summary>
    public CardHand WithCardsAdded(IEnumerable<PlayableCard> cards)
    {
        var newCards = Cards.Concat(cards).Take(MaxHandSize).ToList();
        return new CardHand(newCards);
    }
}
