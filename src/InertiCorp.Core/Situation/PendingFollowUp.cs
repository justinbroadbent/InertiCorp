namespace InertiCorp.Core.Situation;

/// <summary>
/// Types of follow-up events that can occur from played projects.
/// </summary>
public enum FollowUpType
{
    /// <summary>
    /// Positive outcome - just a nice email, no response needed.
    /// </summary>
    Good,

    /// <summary>
    /// Neutral/mild outcome - informational email, no response needed.
    /// Can be mildly positive or mildly negative.
    /// </summary>
    Meh,

    /// <summary>
    /// Crisis - serious situation requiring CEO response.
    /// Uses the existing crisis/situation system.
    /// </summary>
    Crisis
}

/// <summary>
/// Tracks a played project that's eligible for follow-up events.
/// Projects can generate follow-ups for several quarters after being played.
/// </summary>
public sealed record PendingFollowUp(
    string CardId,
    string CardTitle,
    string ThreadId,
    int PlayedAtQuarter,
    OutcomeTier OriginalOutcome)
{
    /// <summary>
    /// Maximum quarters a project stays in the follow-up queue.
    /// </summary>
    public const int MaxQuartersEligible = 3;

    /// <summary>
    /// How many quarters since this project was played.
    /// </summary>
    public int QuartersSincePlayed(int currentQuarter) => currentQuarter - PlayedAtQuarter;

    /// <summary>
    /// Whether this project has expired from the follow-up queue.
    /// </summary>
    public bool HasExpired(int currentQuarter) => QuartersSincePlayed(currentQuarter) > MaxQuartersEligible;

    /// <summary>
    /// Whether this project is still eligible for follow-ups.
    /// </summary>
    public bool IsEligible(int currentQuarter) => !HasExpired(currentQuarter);

    /// <summary>
    /// Creates a pending follow-up from a played card.
    /// </summary>
    public static PendingFollowUp Create(
        string cardId,
        string cardTitle,
        string threadId,
        int currentQuarter,
        OutcomeTier outcome) =>
        new(cardId, cardTitle, threadId, currentQuarter, outcome);
}
