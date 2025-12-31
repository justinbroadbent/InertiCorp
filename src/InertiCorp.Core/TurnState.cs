namespace InertiCorp.Core;

/// <summary>
/// Immutable record tracking the current turn.
/// </summary>
public sealed record TurnState(int TurnNumber)
{
    /// <summary>
    /// Initial turn state (turn 1).
    /// </summary>
    public static TurnState Initial => new(TurnNumber: 1);

    /// <summary>
    /// Whether this is the last turn of the quarter.
    /// </summary>
    public bool IsLastTurn => TurnNumber >= GameConstants.TurnsPerQuarter;

    /// <summary>
    /// Returns a new TurnState with incremented turn number.
    /// </summary>
    public TurnState NextTurn() => new(TurnNumber + 1);
}
