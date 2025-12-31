using InertiCorp.Core.Content;

namespace InertiCorp.Core.Situation;

/// <summary>
/// Handles follow-up event generation for played projects.
/// Each quarter, eligible projects have a chance to generate follow-up events.
/// </summary>
public static class FollowUpResolver
{
    /// <summary>
    /// Base chance (percent) for a follow-up event to trigger each quarter.
    /// </summary>
    public const int BaseFollowUpChance = 20;

    /// <summary>
    /// Additional chance per quarter since played (reflects growing consequences).
    /// </summary>
    public const int ChancePerQuarter = 5;

    /// <summary>
    /// Maximum chance for follow-up (percent).
    /// </summary>
    public const int MaxFollowUpChance = 40;

    /// <summary>
    /// Weights for follow-up type selection (out of 100).
    /// Good: 20%, Meh: 50%, Crisis: 30%
    /// </summary>
    private const int GoodWeight = 20;
    private const int MehWeight = 50;
    // Crisis is the remaining 30%

    /// <summary>
    /// Result of checking a project for follow-up.
    /// </summary>
    public record FollowUpResult(
        PendingFollowUp FollowUp,
        FollowUpType Type,
        string? SituationId = null,
        MeterEffect[]? Effects = null);

    /// <summary>
    /// Checks all pending follow-ups and generates events for this quarter.
    /// Returns list of triggered follow-ups with their types.
    /// </summary>
    public static IReadOnlyList<FollowUpResult> CheckAllFollowUps(
        IReadOnlyList<PendingFollowUp> pendingFollowUps,
        int currentQuarter,
        SeededRng rng)
    {
        var results = new List<FollowUpResult>();

        foreach (var followUp in pendingFollowUps)
        {
            // Skip if expired
            if (followUp.HasExpired(currentQuarter))
                continue;

            // Roll for trigger
            var triggered = CheckForTrigger(followUp, currentQuarter, rng);
            if (triggered is not null)
            {
                results.Add(triggered);
            }
        }

        return results;
    }

    /// <summary>
    /// Checks a single project for follow-up trigger.
    /// </summary>
    public static FollowUpResult? CheckForTrigger(
        PendingFollowUp followUp,
        int currentQuarter,
        SeededRng rng)
    {
        // Calculate trigger chance
        var quartersSince = followUp.QuartersSincePlayed(currentQuarter);
        var triggerChance = Math.Min(MaxFollowUpChance, BaseFollowUpChance + (quartersSince * ChancePerQuarter));

        var roll = rng.NextInt(1, 101);
        if (roll > triggerChance)
            return null;

        // Determine follow-up type
        var typeRoll = rng.NextInt(1, 101);
        var type = DetermineFollowUpType(typeRoll, followUp.OriginalOutcome);

        // Generate effects based on type
        var (situationId, effects) = GenerateFollowUpContent(type, followUp.OriginalOutcome, rng);

        return new FollowUpResult(followUp, type, situationId, effects);
    }

    /// <summary>
    /// Determines follow-up type based on roll and original outcome.
    /// Original outcome influences the distribution slightly.
    /// </summary>
    private static FollowUpType DetermineFollowUpType(int roll, OutcomeTier originalOutcome)
    {
        // Adjust weights based on original outcome
        var goodWeight = GoodWeight;
        var mehWeight = MehWeight;

        switch (originalOutcome)
        {
            case OutcomeTier.Good:
                // Good outcomes more likely to have good follow-ups
                goodWeight += 10;
                mehWeight -= 5;
                break;
            case OutcomeTier.Bad:
                // Bad outcomes more likely to have crisis follow-ups
                goodWeight -= 10;
                mehWeight -= 10;
                break;
        }

        if (roll <= goodWeight)
            return FollowUpType.Good;
        if (roll <= goodWeight + mehWeight)
            return FollowUpType.Meh;
        return FollowUpType.Crisis;
    }

    /// <summary>
    /// Generates content for a follow-up based on its type.
    /// Returns situation ID for crises, or meter effects for good/meh.
    /// </summary>
    private static (string? SituationId, MeterEffect[]? Effects) GenerateFollowUpContent(
        FollowUpType type,
        OutcomeTier originalOutcome,
        SeededRng rng)
    {
        switch (type)
        {
            case FollowUpType.Good:
                // Good follow-ups give small positive effects
                var goodEffects = GenerateGoodEffects(rng);
                return (null, goodEffects);

            case FollowUpType.Meh:
                // Meh can be slightly positive or slightly negative
                var mehEffects = GenerateMehEffects(rng);
                return (null, mehEffects);

            case FollowUpType.Crisis:
                // Crisis triggers a situation
                var situationId = SelectCrisisSituation(originalOutcome, rng);
                return (situationId, null);

            default:
                return (null, null);
        }
    }

    /// <summary>
    /// Generates positive meter effects for Good follow-ups.
    /// </summary>
    private static MeterEffect[] GenerateGoodEffects(SeededRng rng)
    {
        // Pick 1-2 random meters to boost
        var meters = new[] { Meter.Delivery, Meter.Morale, Meter.Governance, Meter.Alignment };
        var meter = meters[rng.NextInt(0, meters.Length)];
        var boost = rng.NextInt(3, 8); // +3 to +7

        return new[] { new MeterEffect(meter, boost) };
    }

    /// <summary>
    /// Generates mild effects for Meh follow-ups (can be positive or negative).
    /// </summary>
    private static MeterEffect[] GenerateMehEffects(SeededRng rng)
    {
        var meters = new[] { Meter.Delivery, Meter.Morale, Meter.Governance, Meter.Alignment };
        var meter = meters[rng.NextInt(0, meters.Length)];

        // 60% chance positive, 40% chance negative
        var isPositive = rng.NextInt(1, 101) <= 60;
        var magnitude = rng.NextInt(2, 6); // 2-5
        var delta = isPositive ? magnitude : -magnitude;

        return new[] { new MeterEffect(meter, delta) };
    }

    /// <summary>
    /// Selects a crisis situation based on original outcome.
    /// </summary>
    private static string SelectCrisisSituation(OutcomeTier originalOutcome, SeededRng rng)
    {
        // Pool of generic situations
        var pool = originalOutcome switch
        {
            OutcomeTier.Bad => new[]
            {
                SituationContent.SIT_KEY_PERFORMER_QUITS,
                SituationContent.SIT_SECURITY_VULNERABILITY,
                SituationContent.SIT_GLASSDOOR_FIRESTORM,
            },
            OutcomeTier.Good => new[]
            {
                SituationContent.SIT_TECH_PRESS_RECOGNITION,
                SituationContent.SIT_EMPLOYEE_ENGAGEMENT_BOOST,
            },
            _ => new[]
            {
                SituationContent.SIT_KEY_PERFORMER_QUITS,
                SituationContent.SIT_GLASSDOOR_FIRESTORM,
            }
        };

        return pool[rng.NextInt(0, pool.Length)];
    }
}

/// <summary>
/// A meter effect from a follow-up event.
/// </summary>
public record MeterEffect(Meter Meter, int Delta);
