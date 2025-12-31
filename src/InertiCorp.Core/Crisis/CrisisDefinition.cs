namespace InertiCorp.Core.Crisis;

/// <summary>
/// Static definition of a crisis type from content.
/// CrisisInstances are created from these definitions at runtime.
/// </summary>
public sealed record CrisisDefinition(
    string CrisisId,
    string Title,
    string Description,
    int Severity,
    IReadOnlyList<string> Tags,
    int DeadlineAfterTurns,
    IReadOnlyDictionary<Meter, int> BaseImpact,
    IReadOnlyDictionary<Meter, int>? OngoingImpact = null,
    int? MinimumSpendToFullyMitigate = null)
{
    /// <summary>
    /// Creates a crisis instance from this definition.
    /// </summary>
    public CrisisInstance CreateInstance(string instanceId, int createdTurn, string originEventId) =>
        new(
            CrisisId: CrisisId,
            InstanceId: instanceId,
            Title: Title,
            Description: Description,
            Severity: Severity,
            Tags: Tags,
            CreatedTurn: createdTurn,
            DeadlineTurn: createdTurn + DeadlineAfterTurns,
            BaseImpact: BaseImpact,
            OngoingImpact: OngoingImpact ?? new Dictionary<Meter, int>(),
            Status: CrisisStatus.Active,
            OriginEventId: originEventId,
            MinimumSpendToFullyMitigate: MinimumSpendToFullyMitigate);
}
