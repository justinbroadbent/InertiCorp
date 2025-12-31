namespace InertiCorp.Core;

/// <summary>
/// Category of log entry.
/// </summary>
public enum LogCategory
{
    Info,
    MeterChange,
    Event,
    Outcome
}

/// <summary>
/// A single log entry describing what happened during a turn.
/// </summary>
public sealed record LogEntry
{
    public LogCategory Category { get; }
    public string Message { get; }
    public Meter? Meter { get; }
    public int? Delta { get; }
    public OutcomeTier? OutcomeTier { get; }

    private LogEntry(LogCategory category, string message, Meter? meter, int? delta, OutcomeTier? outcomeTier = null)
    {
        Category = category;
        Message = message;
        Meter = meter;
        Delta = delta;
        OutcomeTier = outcomeTier;
    }

    /// <summary>
    /// Creates an informational log entry.
    /// </summary>
    public static LogEntry Info(string message) =>
        new(LogCategory.Info, message, null, null);

    /// <summary>
    /// Creates a meter change log entry.
    /// </summary>
    public static LogEntry MeterChange(Meter meter, int delta, string message) =>
        new(LogCategory.MeterChange, message, meter, delta);

    /// <summary>
    /// Creates an event log entry.
    /// </summary>
    public static LogEntry Event(string message) =>
        new(LogCategory.Event, message, null, null);

    /// <summary>
    /// Creates an outcome log entry showing which tier was rolled.
    /// </summary>
    public static LogEntry Outcome(OutcomeTier tier, string cardTitle, string choiceLabel) =>
        new(LogCategory.Outcome, $"[{tier}] {cardTitle}: {choiceLabel}", null, null, tier);
}
