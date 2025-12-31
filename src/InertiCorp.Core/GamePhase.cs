namespace InertiCorp.Core;

/// <summary>
/// Phases within a quarter. Each quarter progresses through all four phases.
/// BoardDemand → PlayCards → Crisis → Resolution
/// Emails are generated immediately during PlayCards and Crisis phases.
/// </summary>
public enum GamePhase
{
    /// <summary>
    /// Board Demand phase - board sets a directive/demand for the quarter.
    /// Board directive arrives as email.
    /// </summary>
    BoardDemand,

    /// <summary>
    /// Play Cards phase - player plays 0-3 cards from their hand.
    /// Each card generates an immediate email response showing results.
    /// </summary>
    PlayCards,

    /// <summary>
    /// Crisis phase - handle an urgent problem via email.
    /// Crisis arrives as email with reply options showing risks/rewards.
    /// </summary>
    Crisis,

    /// <summary>
    /// Resolution phase - calculate profit, board reaction, and ouster vote.
    /// </summary>
    Resolution
}
