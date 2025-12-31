namespace InertiCorp.Core.Crisis;

/// <summary>
/// Result of attempting to resolve a crisis with a response.
/// </summary>
public sealed record CrisisResolutionResult(
    OutcomeTier Outcome,
    StaffQuality AssignedStaff,
    CrisisInstance UpdatedCrisis,
    IReadOnlyDictionary<Meter, int> MeterDeltas,
    IReadOnlyList<string> SpawnedEffects,
    IReadOnlyList<string> ScheduledAftershocks,
    int RollValue,
    int ModifiedRoll,
    string NarrativeSummary);

/// <summary>
/// Resolves crisis responses using deterministic 2d6 rolls with modifiers.
/// </summary>
public static class CrisisResolver
{
    /// <summary>
    /// Success threshold on modified 2d6 roll.
    /// </summary>
    public const int SuccessThreshold = 10;

    /// <summary>
    /// Mixed result threshold on modified 2d6 roll.
    /// </summary>
    public const int MixedThreshold = 6;

    /// <summary>
    /// Resolves a crisis response attempt.
    /// </summary>
    public static CrisisResolutionResult Resolve(
        CrisisInstance crisis,
        CrisisResponse response,
        ResourceState _,
        OrgState org,
        IRng rng)
    {
        // Roll 2d6
        var die1 = rng.NextInt(1, 7);
        var die2 = rng.NextInt(1, 7);
        var baseRoll = die1 + die2;

        // Calculate modifiers
        var mitigationBonus = response.GetTotalMitigationBonus(crisis);
        var alignmentBonus = org.Alignment >= 60 ? 1 : (org.Alignment < 30 ? -1 : 0);
        var severityPenalty = crisis.Severity > 3 ? -(crisis.Severity - 3) : 0;

        var modifiedRoll = baseRoll + mitigationBonus + alignmentBonus + severityPenalty;

        // Determine outcome tier
        var outcome = DetermineOutcome(modifiedRoll);

        // Check if cheap response can achieve full success
        if (outcome == OutcomeTier.Good && !crisis.CanFullyMitigateWith(response.CostPC))
        {
            outcome = OutcomeTier.Expected; // Downgrade to mixed
        }

        // Determine staff quality
        var staffRoll = rng.NextInt(0, 100);
        var staffQuality = response.StaffQuality.DetermineQuality(staffRoll);

        // Get the appropriate outcome definition
        var outcomeSpec = outcome switch
        {
            OutcomeTier.Good => response.Outcomes.Success,
            OutcomeTier.Expected => response.Outcomes.Mixed,
            OutcomeTier.Bad => response.Outcomes.Fail,
            _ => response.Outcomes.Mixed
        };

        // Apply crisis operation
        var updatedCrisis = ApplyCrisisOperation(crisis, outcomeSpec);

        // Collect meter deltas
        var meterDeltas = outcomeSpec.MeterDeltas ?? new Dictionary<Meter, int>();

        // Collect effects (including potential inept PM effect)
        var effects = new List<string>(outcomeSpec.SpawnEffects ?? Array.Empty<string>());
        if (staffQuality == StaffQuality.Inept && outcome != OutcomeTier.Good)
        {
            effects.Add("inept_project_manager");
        }

        // Collect aftershocks
        var aftershocks = new List<string>(outcomeSpec.ScheduleAftershocks ?? Array.Empty<string>());

        // Generate narrative summary
        var narrative = GenerateNarrative(crisis, response, outcome, staffQuality, modifiedRoll);

        return new CrisisResolutionResult(
            Outcome: outcome,
            AssignedStaff: staffQuality,
            UpdatedCrisis: updatedCrisis,
            MeterDeltas: meterDeltas,
            SpawnedEffects: effects,
            ScheduledAftershocks: aftershocks,
            RollValue: baseRoll,
            ModifiedRoll: modifiedRoll,
            NarrativeSummary: narrative);
    }

    private static OutcomeTier DetermineOutcome(int modifiedRoll)
    {
        if (modifiedRoll >= SuccessThreshold)
            return OutcomeTier.Good;
        if (modifiedRoll >= MixedThreshold)
            return OutcomeTier.Expected;
        return OutcomeTier.Bad;
    }

    private static CrisisInstance ApplyCrisisOperation(CrisisInstance crisis, ResponseOutcome outcome)
    {
        return outcome.CrisisOp switch
        {
            CrisisOperation.Mitigate => crisis.WithMitigated(),
            CrisisOperation.ReduceSeverity => crisis.WithReducedSeverity(outcome.SeverityReduction ?? 1),
            CrisisOperation.ExtendDeadline => crisis.WithExtendedDeadline(outcome.DeadlineExtension ?? 1),
            CrisisOperation.Escalate => crisis.WithEscalated(),
            CrisisOperation.None => crisis,
            _ => crisis
        };
    }

    private static string GenerateNarrative(
        CrisisInstance crisis,
        CrisisResponse _,
        OutcomeTier outcome,
        StaffQuality staff,
        int roll)
    {
        var staffDesc = staff switch
        {
            StaffQuality.Good => "Your team executed flawlessly.",
            StaffQuality.Meh => "The team did... okay.",
            StaffQuality.Inept => "The assigned PM somehow made everything worse.",
            _ => "The team performed as expected."
        };

        var outcomeDesc = outcome switch
        {
            OutcomeTier.Good => $"'{crisis.Title}' has been fully resolved.",
            OutcomeTier.Expected => $"'{crisis.Title}' is partially contained, but issues remain.",
            OutcomeTier.Bad => $"'{crisis.Title}' has escalated despite our efforts.",
            _ => $"'{crisis.Title}' situation is unchanged."
        };

        return $"{outcomeDesc} {staffDesc} (Roll: {roll})";
    }

    /// <summary>
    /// Calculates the expected success chance for a response against a crisis.
    /// </summary>
    public static int CalculateSuccessChance(CrisisInstance crisis, CrisisResponse response, OrgState org)
    {
        var mitigationBonus = response.GetTotalMitigationBonus(crisis);
        var alignmentBonus = org.Alignment >= 60 ? 1 : (org.Alignment < 30 ? -1 : 0);
        var severityPenalty = crisis.Severity > 3 ? -(crisis.Severity - 3) : 0;

        var totalModifier = mitigationBonus + alignmentBonus + severityPenalty;

        // 2d6 probabilities adjusted for modifier
        // Need 10+ for success, base chance is 17% (6/36)
        // Each +1 modifier improves odds
        var successThreshold = SuccessThreshold - totalModifier;
        var successChance = Calculate2d6Probability(successThreshold);

        return (int)(successChance * 100);
    }

    private static double Calculate2d6Probability(int targetOrHigher)
    {
        if (targetOrHigher <= 2) return 1.0;
        if (targetOrHigher > 12) return 0.0;

        // Count combinations that meet or exceed target
        int successCombos = 0;
        for (int d1 = 1; d1 <= 6; d1++)
        {
            for (int d2 = 1; d2 <= 6; d2++)
            {
                if (d1 + d2 >= targetOrHigher)
                    successCombos++;
            }
        }

        return successCombos / 36.0;
    }
}
