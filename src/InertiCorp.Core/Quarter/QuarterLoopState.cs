using InertiCorp.Core.Cards;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Quarter;

/// <summary>
/// State for the new quarterly loop system.
/// Tracks phase, selections, and quarter-specific data.
/// </summary>
public sealed record QuarterLoopState(
    int QuarterNumber,
    QuarterPhase Phase,
    IReadOnlyList<string> SelectedProjectIds,
    bool ReorgUsedThisQuarter,
    int QuarterProfit,
    int QuarterFines,
    CrisisInstance? ActiveCrisis,
    BoardReviewResult? BoardReview,
    int ConsecutivePoorQuarters,
    PendingSituation? ActiveSituation = null)
{
    /// <summary>
    /// Initial state for quarter 1.
    /// </summary>
    public static QuarterLoopState Initial => new(
        QuarterNumber: 1,
        Phase: QuarterPhase.Projects,
        SelectedProjectIds: Array.Empty<string>(),
        ReorgUsedThisQuarter: false,
        QuarterProfit: 0,
        QuarterFines: 0,
        ActiveCrisis: null,
        BoardReview: null,
        ConsecutivePoorQuarters: 0);

    /// <summary>
    /// Net profit after fines are deducted.
    /// </summary>
    public int NetProfit => QuarterProfit - QuarterFines;

    /// <summary>
    /// Whether this is the Projects phase.
    /// </summary>
    public bool IsProjectsPhase => Phase == QuarterPhase.Projects;

    /// <summary>
    /// Whether this is the Situation phase.
    /// </summary>
    public bool IsSituationPhase => Phase == QuarterPhase.Situation;

    /// <summary>
    /// Whether this is the Board Meeting phase.
    /// </summary>
    public bool IsBoardMeetingPhase => Phase == QuarterPhase.BoardMeeting;

    /// <summary>
    /// Number of projects selected this quarter.
    /// </summary>
    public int ProjectCount => SelectedProjectIds.Count;

    /// <summary>
    /// Whether the required number of projects have been selected.
    /// </summary>
    public bool ProjectsComplete => ReorgUsedThisQuarter
        ? ProjectCount >= 1
        : ProjectCount >= 3;

    /// <summary>
    /// Returns a new state with a project added to selections.
    /// </summary>
    public QuarterLoopState WithProjectSelected(string cardId) =>
        this with { SelectedProjectIds = SelectedProjectIds.Append(cardId).ToList() };

    /// <summary>
    /// Returns a new state with multiple projects selected.
    /// </summary>
    public QuarterLoopState WithProjectsSelected(IEnumerable<string> cardIds) =>
        this with { SelectedProjectIds = cardIds.ToList() };

    /// <summary>
    /// Returns a new state with reorg used flag set.
    /// </summary>
    public QuarterLoopState WithReorgUsed() =>
        this with { ReorgUsedThisQuarter = true };

    /// <summary>
    /// Returns a new state with profit added.
    /// </summary>
    public QuarterLoopState WithProfitAdded(int profit) =>
        this with { QuarterProfit = QuarterProfit + profit };

    /// <summary>
    /// Returns a new state with a fine added.
    /// </summary>
    public QuarterLoopState WithFineAdded(int fineAmount) =>
        this with { QuarterFines = QuarterFines + fineAmount };

    /// <summary>
    /// Returns a new state with active crisis set.
    /// </summary>
    public QuarterLoopState WithActiveCrisis(CrisisInstance? crisis) =>
        this with { ActiveCrisis = crisis };

    /// <summary>
    /// Returns a new state with board review result set.
    /// </summary>
    public QuarterLoopState WithBoardReview(BoardReviewResult review) =>
        this with { BoardReview = review };

    /// <summary>
    /// Returns a new state advanced to the next phase.
    /// </summary>
    public QuarterLoopState NextPhase()
    {
        return Phase switch
        {
            QuarterPhase.Projects => this with { Phase = QuarterPhase.Situation },
            QuarterPhase.Situation => this with { Phase = QuarterPhase.BoardMeeting, ActiveSituation = null },
            QuarterPhase.BoardMeeting => StartNextQuarter(),
            _ => throw new InvalidOperationException($"Unknown phase: {Phase}")
        };
    }

    /// <summary>
    /// Returns a new state with the active situation set.
    /// </summary>
    public QuarterLoopState WithActiveSituation(PendingSituation? situation) =>
        this with { ActiveSituation = situation };

    /// <summary>
    /// Creates the initial state for the next quarter.
    /// </summary>
    private QuarterLoopState StartNextQuarter()
    {
        var poorQuarters = BoardReview is not null && BoardReviewCalculator.IsPoorRating(BoardReview.Rating)
            ? ConsecutivePoorQuarters + 1
            : 0;

        return new QuarterLoopState(
            QuarterNumber: QuarterNumber + 1,
            Phase: QuarterPhase.Projects,
            SelectedProjectIds: Array.Empty<string>(),
            ReorgUsedThisQuarter: false,
            QuarterProfit: 0,
            QuarterFines: 0,
            ActiveCrisis: null,
            BoardReview: null,
            ConsecutivePoorQuarters: poorQuarters);
    }
}
