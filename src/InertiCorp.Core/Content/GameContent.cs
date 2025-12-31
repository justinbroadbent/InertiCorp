using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Content;

/// <summary>
/// Aggregates all game content for easy access.
/// Provides access to Crisis, Board, and Project decks for the CEO survival game.
/// </summary>
public static class GameContent
{
    /// <summary>
    /// All crisis event cards (drawn during Crisis phase).
    /// Loaded from embedded JSON resource for easy content updates.
    /// </summary>
    public static IReadOnlyList<EventCard> CrisisCards { get; } = CardContentLoader.CrisisCards;

    /// <summary>
    /// All board event cards (kept for potential future use).
    /// </summary>
    public static IReadOnlyList<EventCard> BoardCards { get; } = BoardEvents.All;

    /// <summary>
    /// All project event cards (kept for potential future use).
    /// </summary>
    public static IReadOnlyList<EventCard> ProjectCards { get; } = ProjectEvents.All;

    /// <summary>
    /// All playable cards for the player's deck.
    /// Loaded from embedded JSON resource for easy content updates.
    /// </summary>
    public static IReadOnlyList<PlayableCard> PlayableCardDeck { get; } = CardContentLoader.ProjectCards;

    /// <summary>
    /// All event cards (legacy support - combines all decks).
    /// </summary>
    public static IReadOnlyList<EventCard> AllEvents { get; } =
        CrisisCards
            .Concat(BoardCards)
            .Concat(ProjectCards)
            .ToList();

    /// <summary>
    /// All objectives (legacy support).
    /// </summary>
    public static IReadOnlyList<Objective> AllObjectives { get; } =
        GameObjectives.All;

    /// <summary>
    /// Creates a DeckSet with Crisis, Board, and Project decks.
    /// </summary>
    public static DeckSet CreateDeckSet() => new DeckSet(
        new EventDeck(CrisisCards),
        new EventDeck(BoardCards),
        new EventDeck(ProjectCards)
    );

    /// <summary>
    /// Creates an event deck with all events (legacy support).
    /// </summary>
    public static EventDeck CreateDeck() => new EventDeck(AllEvents);

    /// <summary>
    /// Creates an objective pool with all objectives (legacy support).
    /// </summary>
    public static ObjectivePool CreateObjectivePool() => new ObjectivePool(AllObjectives);
}
