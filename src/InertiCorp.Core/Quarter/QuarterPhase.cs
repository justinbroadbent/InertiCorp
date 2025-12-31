namespace InertiCorp.Core.Quarter;

/// <summary>
/// The three phases of each quarter in the new quarterly loop.
/// Projects → Situation → BoardMeeting
/// </summary>
public enum QuarterPhase
{
    /// <summary>
    /// Projects phase - select 3 projects from hand OR reorg and select 1.
    /// This is where the player makes strategic project choices.
    /// </summary>
    Projects,

    /// <summary>
    /// Situation phase - handle situations requiring CEO attention.
    /// Includes: card-triggered situations, random crisis draws (33% chance), and deferred situations.
    /// Player chooses PC spend, Risk roll, Evil option, or Defer.
    /// </summary>
    Situation,

    /// <summary>
    /// Board Meeting phase - quarterly review, rating, and employment decision.
    /// Player can spend Political Capital to influence the board.
    /// </summary>
    BoardMeeting
}

/// <summary>
/// Player's choice for the Projects phase.
/// </summary>
public abstract record ProjectsPhaseChoice
{
    /// <summary>
    /// Select exactly 3 projects from hand.
    /// </summary>
    public sealed record SelectProjects(IReadOnlyList<string> CardIds) : ProjectsPhaseChoice
    {
        public const int RequiredCount = 3;

        public bool IsValid => CardIds.Count == RequiredCount && CardIds.Distinct().Count() == RequiredCount;
    }

    /// <summary>
    /// Reorg (redraw hand) then select exactly 1 project.
    /// </summary>
    public sealed record ReorgAndSelectOne(string CardId) : ProjectsPhaseChoice;
}

/// <summary>
/// Player's choice for the Board Meeting phase.
/// </summary>
public abstract record BoardMeetingChoice
{
    /// <summary>
    /// Accept the board's decision without influence.
    /// </summary>
    public sealed record Accept : BoardMeetingChoice;

    /// <summary>
    /// Spend Political Capital to influence the board.
    /// </summary>
    public sealed record Influence(string InfluencePackageId) : BoardMeetingChoice;
}

/// <summary>
/// Player's choice for the Situation phase.
/// </summary>
public abstract record SituationPhaseChoice
{
    /// <summary>
    /// Spend Political Capital to address the situation professionally.
    /// High success rate (70% good).
    /// </summary>
    public sealed record SpendPC : SituationPhaseChoice;

    /// <summary>
    /// Roll the dice - balanced outcomes (40% good, 40% expected, 20% bad).
    /// No cost, but risky.
    /// </summary>
    public sealed record RollDice : SituationPhaseChoice;

    /// <summary>
    /// Use questionable methods - effective but increases evil score.
    /// Good outcomes (60% good) but increases EvilScore.
    /// </summary>
    public sealed record EvilOption : SituationPhaseChoice;

    /// <summary>
    /// Defer the situation - put it aside for later.
    /// May resurface with escalated severity.
    /// Cannot defer Critical situations.
    /// </summary>
    public sealed record Defer : SituationPhaseChoice;

    /// <summary>
    /// Skip the situation phase when there are no situations to handle.
    /// </summary>
    public sealed record Skip : SituationPhaseChoice;
}
