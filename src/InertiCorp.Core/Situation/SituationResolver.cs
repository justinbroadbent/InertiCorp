using InertiCorp.Core.Content;

namespace InertiCorp.Core.Situation;

/// <summary>
/// Handles all dice rolls and resolution logic for situations.
/// </summary>
public static class SituationResolver
{
    /// <summary>
    /// Rolls to determine if a card triggers a situation and when.
    /// Returns null if no situation triggers.
    /// </summary>
    /// <param name="cardSituations">The card's situation mappings</param>
    /// <param name="outcome">The outcome tier of the card play</param>
    /// <param name="currentQuarter">Current quarter number</param>
    /// <param name="rng">Seeded random number generator</param>
    /// <param name="originatingThreadId">The email thread ID to link situation emails to</param>
    /// <returns>Pending situation if triggered, null otherwise</returns>
    public static PendingSituation? CheckForTrigger(
        CardSituations cardSituations,
        OutcomeTier outcome,
        int currentQuarter,
        SeededRng rng,
        string? originatingThreadId = null)
    {
        // Roll d20 for trigger and timing
        var triggerRoll = rng.NextInt(1, 21);

        // 18-20: No trigger (25% chance)
        if (triggerRoll >= 18) return null;

        // Select which situation triggers based on weighted random
        var trigger = cardSituations.SelectTrigger(outcome, rng);
        if (trigger is null) return null;

        // Determine delay based on roll
        var delay = triggerRoll switch
        {
            >= 1 and <= 5 => 0,   // Immediate (25%)
            >= 6 and <= 10 => 1,  // Next quarter (25%)
            >= 11 and <= 14 => 2, // 2 quarters (20%)
            >= 15 and <= 17 => 3, // 3 quarters (15%)
            _ => 0
        };

        return PendingSituation.Create(
            trigger.SituationId,
            cardSituations.CardId,
            currentQuarter,
            delay,
            originatingThreadId);
    }

    /// <summary>
    /// Generic situation pool - situations that can trigger from any card.
    /// </summary>
    private static readonly string[] GenericBadSituations = new[]
    {
        SituationContent.SIT_KEY_PERFORMER_QUITS,
        SituationContent.SIT_GLASSDOOR_FIRESTORM,
        SituationContent.SIT_SECURITY_VULNERABILITY,
    };

    private static readonly string[] GenericExpectedSituations = new[]
    {
        SituationContent.SIT_KEY_PERFORMER_QUITS,
        SituationContent.SIT_GLASSDOOR_FIRESTORM,
    };

    private static readonly string[] GenericGoodSituations = new[]
    {
        SituationContent.SIT_EMPLOYEE_ENGAGEMENT_BOOST,
        SituationContent.SIT_TECH_PRESS_RECOGNITION,
    };

    /// <summary>
    /// Checks if a card triggers a generic situation (for cards without specific mappings).
    /// Probability scales with quarters played: 5% base + 2% per quarter (max 25%).
    /// </summary>
    public static PendingSituation? CheckGenericTrigger(
        string cardId,
        OutcomeTier outcome,
        int currentQuarter,
        SeededRng rng,
        string? originatingThreadId = null)
    {
        // Calculate trigger chance: 5% base + 2% per quarter, max 25%
        var triggerChance = Math.Min(25, 5 + (currentQuarter * 2));
        var roll = rng.NextInt(1, 101);

        if (roll > triggerChance) return null;

        // Select situation pool based on outcome
        var pool = outcome switch
        {
            OutcomeTier.Bad => GenericBadSituations,
            OutcomeTier.Expected => GenericExpectedSituations,
            OutcomeTier.Good => GenericGoodSituations,
            _ => Array.Empty<string>()
        };

        if (pool.Length == 0) return null;

        // Random selection from pool
        var situationId = pool[rng.NextInt(0, pool.Length)];

        // Determine delay (similar to specific triggers but simpler)
        var delayRoll = rng.NextInt(1, 11);
        var delay = delayRoll switch
        {
            >= 1 and <= 4 => 0,   // Immediate (40%)
            >= 5 and <= 7 => 1,   // Next quarter (30%)
            >= 8 and <= 9 => 2,   // 2 quarters (20%)
            _ => 3                 // 3 quarters (10%)
        };

        return PendingSituation.Create(
            situationId,
            cardId,
            currentQuarter,
            delay,
            originatingThreadId);
    }

    /// <summary>
    /// Checks if a delayed situation still triggers (decay roll).
    /// </summary>
    /// <param name="pending">The pending situation</param>
    /// <param name="currentQuarter">Current quarter number</param>
    /// <param name="rng">Seeded random number generator</param>
    /// <returns>True if situation should fire, false if it fades</returns>
    public static bool CheckDecay(PendingSituation pending, int currentQuarter, SeededRng rng)
    {
        var quartersWaiting = pending.QuartersWaiting(currentQuarter);

        // Immediate triggers always fire
        if (quartersWaiting <= 0) return true;

        // Decay probability based on waiting time
        var survivalChance = quartersWaiting switch
        {
            1 => 80,   // 80% chance still triggers
            2 => 60,   // 60% chance
            3 => 40,   // 40% chance
            _ => 20    // 20% or fades away
        };

        var roll = rng.NextInt(1, 101);
        return roll <= survivalChance;
    }

    /// <summary>
    /// Checks if a deferred situation resurfaces this quarter.
    /// </summary>
    /// <param name="pending">The deferred situation</param>
    /// <param name="rng">Seeded random number generator</param>
    /// <returns>True if situation resurfaces</returns>
    public static bool CheckResurface(PendingSituation pending, SeededRng rng)
    {
        // 30% chance per quarter to resurface
        var roll = rng.NextInt(1, 101);
        return roll <= 30;
    }

    /// <summary>
    /// Checks if a deferred situation should fade away (self-resolved).
    /// </summary>
    /// <param name="pending">The deferred situation</param>
    /// <param name="currentQuarter">Current quarter number</param>
    /// <returns>True if situation should be removed from queue</returns>
    public static bool ShouldFade(PendingSituation pending, int currentQuarter)
    {
        // After 4 quarters in deferred queue, situation fades
        return pending.QuartersWaiting(currentQuarter) >= 4;
    }

    /// <summary>
    /// Rolls outcome for a situation response.
    /// </summary>
    public static OutcomeTier RollResponseOutcome(ResponseType responseType, SeededRng rng)
    {
        var weights = SituationResponse.GetWeightsForType(responseType);
        var roll = rng.NextInt(1, 101);

        if (roll <= weights.Good) return OutcomeTier.Good;
        if (roll <= weights.Good + weights.Expected) return OutcomeTier.Expected;
        return OutcomeTier.Bad;
    }

    /// <summary>
    /// Resolves a situation response, returning effects to apply.
    /// </summary>
    public static SituationResolutionResult ResolveResponse(
        SituationDefinition situation,
        ResponseType responseType,
        SeededRng rng)
    {
        // Defer has special handling
        if (responseType == ResponseType.Defer)
        {
            return new SituationResolutionResult(
                Outcome: OutcomeTier.Expected,
                Effects: Array.Empty<IEffect>(),
                WasDeferred: true,
                EvilDelta: 0,
                PCSpent: 0);
        }

        var response = situation.GetResponse(responseType);
        var outcome = RollResponseOutcome(responseType, rng);

        var effects = outcome switch
        {
            OutcomeTier.Good => response.Outcomes.Good,
            OutcomeTier.Expected => response.Outcomes.Expected,
            OutcomeTier.Bad => response.Outcomes.Bad,
            _ => response.Outcomes.Expected
        };

        return new SituationResolutionResult(
            Outcome: outcome,
            Effects: effects,
            WasDeferred: false,
            EvilDelta: response.EvilDelta,
            PCSpent: response.PCCost ?? 0);
    }
}

/// <summary>
/// Result of resolving a situation response.
/// </summary>
public sealed record SituationResolutionResult(
    OutcomeTier Outcome,
    IReadOnlyList<IEffect> Effects,
    bool WasDeferred,
    int EvilDelta,
    int PCSpent);
