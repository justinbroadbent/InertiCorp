namespace InertiCorp.Core.Situation;

/// <summary>
/// Defines when a situation can be triggered by a card play.
/// </summary>
public sealed record SituationTrigger(
    string SituationId,
    OutcomeTier? OnOutcome,
    int TriggerWeight)
{
    /// <summary>
    /// Whether this trigger matches the given outcome tier.
    /// </summary>
    public bool MatchesOutcome(OutcomeTier outcome) =>
        OnOutcome is null || OnOutcome == outcome;
}

/// <summary>
/// Maps a card to its possible situations.
/// </summary>
public sealed record CardSituations(
    string CardId,
    IReadOnlyList<SituationTrigger> PossibleSituations)
{
    /// <summary>
    /// Gets triggers that match the given outcome tier.
    /// </summary>
    public IReadOnlyList<SituationTrigger> GetMatchingTriggers(OutcomeTier outcome) =>
        PossibleSituations.Where(t => t.MatchesOutcome(outcome)).ToList();

    /// <summary>
    /// Selects a situation using weighted random selection.
    /// </summary>
    public SituationTrigger? SelectTrigger(OutcomeTier outcome, SeededRng rng)
    {
        var matching = GetMatchingTriggers(outcome);
        if (matching.Count == 0) return null;

        var totalWeight = matching.Sum(t => t.TriggerWeight);
        var roll = rng.NextInt(1, totalWeight + 1);

        var cumulative = 0;
        foreach (var trigger in matching)
        {
            cumulative += trigger.TriggerWeight;
            if (roll <= cumulative) return trigger;
        }

        return matching.Last();
    }
}
