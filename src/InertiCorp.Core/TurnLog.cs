namespace InertiCorp.Core;

/// <summary>
/// Log of what happened during a turn.
/// </summary>
public sealed record TurnLog
{
    /// <summary>
    /// The turn number this log is for.
    /// </summary>
    public int TurnNumber { get; }

    /// <summary>
    /// The ID of the event that was drawn this turn (null if no event).
    /// </summary>
    public string? DrawnEventId { get; }

    /// <summary>
    /// The ID of the choice the player made (null if no choice).
    /// </summary>
    public string? ChosenChoiceId { get; }

    /// <summary>
    /// The log entries for this turn.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries { get; }

    public TurnLog(int turnNumber, IReadOnlyList<LogEntry> entries, string? drawnEventId = null, string? chosenChoiceId = null)
    {
        TurnNumber = turnNumber;
        Entries = entries;
        DrawnEventId = drawnEventId;
        ChosenChoiceId = chosenChoiceId;
    }

    /// <summary>
    /// Creates an empty log for the given turn.
    /// </summary>
    public static TurnLog Empty(int turnNumber) =>
        new(turnNumber, Array.Empty<LogEntry>());
}
