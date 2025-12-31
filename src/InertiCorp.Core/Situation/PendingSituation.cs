namespace InertiCorp.Core.Situation;

/// <summary>
/// A situation that is queued to fire in a future quarter.
/// </summary>
public sealed record PendingSituation(
    string SituationId,
    string OriginCardId,
    int ScheduledQuarter,
    int QueuedAtQuarter,
    int DeferCount = 0,
    string? OriginatingThreadId = null)
{
    /// <summary>
    /// How many quarters this situation has been waiting.
    /// </summary>
    public int QuartersWaiting(int currentQuarter) => currentQuarter - QueuedAtQuarter;

    /// <summary>
    /// Whether this situation should trigger in the given quarter.
    /// </summary>
    public bool IsDueAt(int quarter) => ScheduledQuarter <= quarter;

    /// <summary>
    /// Creates a new pending situation with deferred status (for when player chooses defer).
    /// </summary>
    public PendingSituation WithDeferred(int currentQuarter) =>
        this with
        {
            ScheduledQuarter = currentQuarter + 1, // May resurface next quarter
            DeferCount = DeferCount + 1
        };

    /// <summary>
    /// Creates from a trigger roll result.
    /// </summary>
    public static PendingSituation Create(
        string situationId,
        string originCardId,
        int currentQuarter,
        int delayQuarters,
        string? originatingThreadId = null) =>
        new(
            SituationId: situationId,
            OriginCardId: originCardId,
            ScheduledQuarter: currentQuarter + delayQuarters,
            QueuedAtQuarter: currentQuarter,
            DeferCount: 0,
            OriginatingThreadId: originatingThreadId);
}
