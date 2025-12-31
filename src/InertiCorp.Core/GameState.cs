namespace InertiCorp.Core;

/// <summary>
/// Immutable record representing the complete game state.
/// </summary>
public sealed record GameState
{
    /// <summary>
    /// The seed used to initialize this game (for reproducibility).
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// The organization's current state (meters).
    /// </summary>
    public OrgState Org { get; init; }

    /// <summary>
    /// The current turn state.
    /// </summary>
    public TurnState Turn { get; init; }

    /// <summary>
    /// Whether the game has been lost (Runway or Morale hit 0).
    /// </summary>
    public bool IsLost { get; init; }

    /// <summary>
    /// Whether the game has been won (2+ objectives met at end of quarter).
    /// </summary>
    public bool IsWon { get; init; }

    /// <summary>
    /// The event deck (null if no events in this game).
    /// </summary>
    public EventDeck? Deck { get; init; }

    /// <summary>
    /// The active objectives for this game (drawn at game start).
    /// </summary>
    public IReadOnlyList<Objective> ActiveObjectives { get; init; }

    /// <summary>
    /// Results of objective evaluation at end of game (empty until turn 12 completes).
    /// </summary>
    public IReadOnlyList<ObjectiveResult> ObjectiveResults { get; init; }

    private GameState(
        int seed,
        OrgState org,
        TurnState turn,
        bool isLost,
        bool isWon,
        EventDeck? deck,
        IReadOnlyList<Objective> activeObjectives,
        IReadOnlyList<ObjectiveResult> objectiveResults)
    {
        Seed = seed;
        Org = org;
        Turn = turn;
        IsLost = isLost;
        IsWon = isWon;
        Deck = deck;
        ActiveObjectives = activeObjectives;
        ObjectiveResults = objectiveResults;
    }

    /// <summary>
    /// Creates a new game with default state (no event deck, no objectives).
    /// </summary>
    public static GameState NewGame(int seed)
    {
        return new GameState(
            seed: seed,
            org: OrgState.Default,
            turn: TurnState.Initial,
            isLost: false,
            isWon: false,
            deck: null,
            activeObjectives: Array.Empty<Objective>(),
            objectiveResults: Array.Empty<ObjectiveResult>()
        );
    }

    /// <summary>
    /// Creates a new game with an event deck (no objectives).
    /// </summary>
    public static GameState NewGame(int seed, EventDeck deck)
    {
        return new GameState(
            seed: seed,
            org: OrgState.Default,
            turn: TurnState.Initial,
            isLost: false,
            isWon: false,
            deck: deck,
            activeObjectives: Array.Empty<Objective>(),
            objectiveResults: Array.Empty<ObjectiveResult>()
        );
    }

    /// <summary>
    /// Creates a new game with an objective pool (draws 3 objectives).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if pool has fewer than 3 objectives.</exception>
    public static GameState NewGame(int seed, ObjectivePool objectivePool)
    {
        var rng = new SeededRng(seed);
        var objectives = objectivePool.Draw(3, rng);

        return new GameState(
            seed: seed,
            org: OrgState.Default,
            turn: TurnState.Initial,
            isLost: false,
            isWon: false,
            deck: null,
            activeObjectives: objectives,
            objectiveResults: Array.Empty<ObjectiveResult>()
        );
    }

    /// <summary>
    /// Creates a new game with an event deck and objective pool.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if pool has fewer than 3 objectives.</exception>
    public static GameState NewGame(int seed, EventDeck deck, ObjectivePool objectivePool)
    {
        var rng = new SeededRng(seed);
        var objectives = objectivePool.Draw(3, rng);

        return new GameState(
            seed: seed,
            org: OrgState.Default,
            turn: TurnState.Initial,
            isLost: false,
            isWon: false,
            deck: deck,
            activeObjectives: objectives,
            objectiveResults: Array.Empty<ObjectiveResult>()
        );
    }

    /// <summary>
    /// Returns a new GameState with the specified OrgState.
    /// </summary>
    public GameState WithOrg(OrgState org) =>
        new(Seed, org, Turn, IsLost, IsWon, Deck, ActiveObjectives, ObjectiveResults);

    /// <summary>
    /// Returns a new GameState with the specified TurnState.
    /// </summary>
    public GameState WithTurn(TurnState turn) =>
        new(Seed, Org, turn, IsLost, IsWon, Deck, ActiveObjectives, ObjectiveResults);

    /// <summary>
    /// Returns a new GameState with IsLost set to true.
    /// </summary>
    public GameState WithLoss() =>
        new(Seed, Org, Turn, isLost: true, isWon: false, Deck, ActiveObjectives, ObjectiveResults);

    /// <summary>
    /// Returns a new GameState with win status and objective results.
    /// </summary>
    public GameState WithWinEvaluation(bool isWon, IReadOnlyList<ObjectiveResult> results) =>
        new(Seed, Org, Turn, IsLost, isWon, Deck, ActiveObjectives, results);
}
