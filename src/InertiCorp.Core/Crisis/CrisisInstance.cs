namespace InertiCorp.Core.Crisis;

/// <summary>
/// A specific instance of a crisis occurring in the game.
/// Created from CrisisDefinition when triggered by events/consequences.
/// </summary>
public sealed record CrisisInstance(
    string CrisisId,
    string InstanceId,
    string Title,
    string Description,
    int Severity,
    IReadOnlyList<string> Tags,
    int CreatedTurn,
    int DeadlineTurn,
    IReadOnlyDictionary<Meter, int> BaseImpact,
    IReadOnlyDictionary<Meter, int> OngoingImpact,
    CrisisStatus Status,
    string OriginEventId,
    int? MinimumSpendToFullyMitigate = null)
{
    /// <summary>
    /// Whether this crisis is still active and needs attention.
    /// </summary>
    public bool IsActive => Status == CrisisStatus.Active;

    /// <summary>
    /// Whether the deadline has passed for the given turn.
    /// </summary>
    public bool IsOverdue(int currentTurn) => currentTurn > DeadlineTurn;

    /// <summary>
    /// Turns remaining until deadline.
    /// </summary>
    public int TurnsRemaining(int currentTurn) => Math.Max(0, DeadlineTurn - currentTurn);

    /// <summary>
    /// Whether a given PC spend can fully mitigate this crisis.
    /// Some crises require minimum spend for full mitigation.
    /// </summary>
    public bool CanFullyMitigateWith(int pcSpend) =>
        MinimumSpendToFullyMitigate is null || pcSpend >= MinimumSpendToFullyMitigate;

    /// <summary>
    /// Returns a new instance with reduced severity.
    /// </summary>
    public CrisisInstance WithReducedSeverity(int amount) =>
        this with { Severity = Math.Max(1, Severity - amount) };

    /// <summary>
    /// Returns a new instance with extended deadline.
    /// </summary>
    public CrisisInstance WithExtendedDeadline(int additionalTurns) =>
        this with { DeadlineTurn = DeadlineTurn + additionalTurns };

    /// <summary>
    /// Returns a new instance marked as mitigated.
    /// </summary>
    public CrisisInstance WithMitigated() =>
        this with { Status = CrisisStatus.Mitigated };

    /// <summary>
    /// Returns a new instance marked as escalated.
    /// </summary>
    public CrisisInstance WithEscalated() =>
        this with { Status = CrisisStatus.Escalated };

    /// <summary>
    /// Returns a new instance marked as expired.
    /// </summary>
    public CrisisInstance WithExpired() =>
        this with { Status = CrisisStatus.Expired };

    /// <summary>
    /// Returns a new instance with updated status.
    /// </summary>
    public CrisisInstance WithStatus(CrisisStatus status) =>
        this with { Status = status };
}
