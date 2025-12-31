namespace InertiCorp.Core;

/// <summary>
/// Log of events for a phase advance.
/// </summary>
public sealed record QuarterLog(
    int QuarterNumber,
    GamePhase Phase,
    IReadOnlyList<LogEntry> Entries)
{
    /// <summary>
    /// Creates an empty log for the given quarter and phase.
    /// </summary>
    public static QuarterLog Create(int quarterNumber, GamePhase phase) =>
        new(quarterNumber, phase, Array.Empty<LogEntry>());

    /// <summary>
    /// Returns a new log with the entry added.
    /// </summary>
    public QuarterLog WithEntry(LogEntry entry) =>
        new(QuarterNumber, Phase, Entries.Append(entry).ToList());

    /// <summary>
    /// Returns a new log with multiple entries added.
    /// </summary>
    public QuarterLog WithEntries(IEnumerable<LogEntry> entries) =>
        new(QuarterNumber, Phase, Entries.Concat(entries).ToList());
}
