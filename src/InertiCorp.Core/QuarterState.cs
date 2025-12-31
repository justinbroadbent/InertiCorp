namespace InertiCorp.Core;

/// <summary>
/// Immutable record tracking the current quarter and phase.
/// </summary>
public sealed record QuarterState(int QuarterNumber, GamePhase Phase)
{
    /// <summary>
    /// Initial state (Quarter 1, BoardDemand phase).
    /// </summary>
    public static QuarterState Initial => new(QuarterNumber: 1, Phase: GamePhase.BoardDemand);

    /// <summary>
    /// Gets the fiscal year (1-indexed, 4 quarters per year).
    /// </summary>
    public int FiscalYear => (QuarterNumber - 1) / 4 + 1;

    /// <summary>
    /// Gets the quarter within the fiscal year (1-4).
    /// </summary>
    public int QuarterInYear => (QuarterNumber - 1) % 4 + 1;

    /// <summary>
    /// Gets the quarter formatted as "Y1Q2" style.
    /// </summary>
    public string FormattedQuarter => $"Y{FiscalYear}Q{QuarterInYear}";

    /// <summary>
    /// Formats any quarter number as "Y1Q2" style.
    /// </summary>
    public static string FormatQuarter(int quarterNumber)
    {
        var year = (quarterNumber - 1) / 4 + 1;
        var quarter = (quarterNumber - 1) % 4 + 1;
        return $"Y{year}Q{quarter}";
    }

    /// <summary>
    /// Whether this is the Resolution phase (end of quarter calculations).
    /// </summary>
    public bool IsResolutionPhase => Phase == GamePhase.Resolution;

    /// <summary>
    /// Whether this is a phase requiring player choice (Crisis or PlayCards).
    /// </summary>
    public bool IsPlayerChoicePhase => Phase == GamePhase.Crisis || Phase == GamePhase.PlayCards;

    /// <summary>
    /// Whether this is the crisis phase where player responds to an event.
    /// </summary>
    public bool IsCrisisPhase => Phase == GamePhase.Crisis;

    /// <summary>
    /// Whether this is the play cards phase where player selects cards from hand.
    /// </summary>
    public bool IsPlayCardsPhase => Phase == GamePhase.PlayCards;

    /// <summary>
    /// Whether this is the board demand phase (passive display).
    /// </summary>
    public bool IsBoardDemandPhase => Phase == GamePhase.BoardDemand;

    /// <summary>
    /// Returns a new QuarterState advanced to the next phase.
    /// Order: BoardDemand → PlayCards → Crisis → Resolution → next quarter
    /// </summary>
    public QuarterState NextPhase()
    {
        return Phase switch
        {
            GamePhase.BoardDemand => new QuarterState(QuarterNumber, GamePhase.PlayCards),
            GamePhase.PlayCards => new QuarterState(QuarterNumber, GamePhase.Crisis),
            GamePhase.Crisis => new QuarterState(QuarterNumber, GamePhase.Resolution),
            GamePhase.Resolution => new QuarterState(QuarterNumber + 1, GamePhase.BoardDemand),
            _ => throw new InvalidOperationException($"Unknown phase: {Phase}")
        };
    }
}
