namespace InertiCorp.Core.Crisis;

/// <summary>
/// A purchasable response package for mitigating a crisis.
/// Cost in Political Capital affects success odds and staff quality.
/// </summary>
public sealed record CrisisResponse(
    string ResponseId,
    string Title,
    string Description,
    int CostPC,
    int MitigationBonus,
    StaffQualityWeights StaffQuality,
    ResponseOutcomes Outcomes,
    bool IsOneShot = false,
    int? Cooldown = null)
{
    /// <summary>
    /// Whether this response requires a minimum severity to be useful.
    /// </summary>
    public int? MinimumSeverity { get; init; }

    /// <summary>
    /// Tags that make this response more effective (e.g., "technical" for tech crises).
    /// </summary>
    public IReadOnlyList<string> EffectiveTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Bonus mitigation for matching tags.
    /// </summary>
    public int GetTotalMitigationBonus(CrisisInstance crisis)
    {
        var tagBonus = EffectiveTags.Any(t => crisis.Tags.Contains(t)) ? 1 : 0;
        return MitigationBonus + tagBonus;
    }
}

/// <summary>
/// Weights for determining staff quality assignment.
/// Higher inept weight = more likely to get an incompetent PM.
/// </summary>
public sealed record StaffQualityWeights(
    int IneptWeight,
    int MehWeight,
    int GoodWeight)
{
    /// <summary>
    /// Total weight for normalization.
    /// </summary>
    public int TotalWeight => IneptWeight + MehWeight + GoodWeight;

    /// <summary>
    /// Determines staff quality deterministically based on a roll value (0-99).
    /// </summary>
    public StaffQuality DetermineQuality(int roll)
    {
        var normalized = roll % TotalWeight;

        if (normalized < IneptWeight)
            return StaffQuality.Inept;
        if (normalized < IneptWeight + MehWeight)
            return StaffQuality.Meh;
        return StaffQuality.Good;
    }

    /// <summary>
    /// Premium response: low inept chance, high good chance.
    /// </summary>
    public static StaffQualityWeights Premium => new(5, 25, 70);

    /// <summary>
    /// Standard response: balanced quality.
    /// </summary>
    public static StaffQualityWeights Standard => new(20, 50, 30);

    /// <summary>
    /// Budget response: high inept chance.
    /// </summary>
    public static StaffQualityWeights Budget => new(60, 30, 10);
}

/// <summary>
/// Outcomes for each result tier of a crisis response.
/// </summary>
public sealed record ResponseOutcomes(
    ResponseOutcome Success,
    ResponseOutcome Mixed,
    ResponseOutcome Fail);

/// <summary>
/// A specific outcome for a response result.
/// </summary>
public sealed record ResponseOutcome(
    CrisisOperation CrisisOp,
    IReadOnlyDictionary<Meter, int>? MeterDeltas = null,
    IReadOnlyList<string>? SpawnEffects = null,
    IReadOnlyList<string>? ScheduleAftershocks = null,
    int? SeverityReduction = null,
    int? DeadlineExtension = null);

/// <summary>
/// Operation to perform on the crisis after response resolution.
/// </summary>
public enum CrisisOperation
{
    /// <summary>
    /// No change to crisis status.
    /// </summary>
    None,

    /// <summary>
    /// Fully mitigate the crisis.
    /// </summary>
    Mitigate,

    /// <summary>
    /// Reduce crisis severity.
    /// </summary>
    ReduceSeverity,

    /// <summary>
    /// Extend the deadline.
    /// </summary>
    ExtendDeadline,

    /// <summary>
    /// Escalate the crisis (make it worse).
    /// </summary>
    Escalate
}
