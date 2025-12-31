namespace InertiCorp.Core;

/// <summary>
/// Profile containing effects for each outcome tier (Good/Expected/Bad).
/// </summary>
public sealed record OutcomeProfile(
    IReadOnlyList<IEffect> Good,
    IReadOnlyList<IEffect> Expected,
    IReadOnlyList<IEffect> Bad)
{
    /// <summary>
    /// Gets the effects for the specified tier.
    /// </summary>
    public IReadOnlyList<IEffect> GetEffectsForTier(OutcomeTier tier) => tier switch
    {
        OutcomeTier.Good => Good,
        OutcomeTier.Expected => Expected,
        OutcomeTier.Bad => Bad,
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    /// <summary>
    /// Rolls for an outcome tier based on weights.
    /// </summary>
    /// <param name="rng">Random number generator.</param>
    /// <param name="goodWeight">Weight for Good outcome (0-100).</param>
    /// <param name="expectedWeight">Weight for Expected outcome (0-100).</param>
    /// <param name="badWeight">Weight for Bad outcome (0-100).</param>
    /// <returns>The selected outcome tier.</returns>
    public OutcomeTier Roll(IRng rng, int goodWeight, int expectedWeight, int badWeight)
    {
        int total = goodWeight + expectedWeight + badWeight;
        if (total == 0)
        {
            return OutcomeTier.Expected;
        }

        int roll = rng.NextInt(0, total);

        if (roll < goodWeight)
        {
            return OutcomeTier.Good;
        }
        else if (roll < goodWeight + expectedWeight)
        {
            return OutcomeTier.Expected;
        }
        else
        {
            return OutcomeTier.Bad;
        }
    }

    /// <summary>
    /// Creates a simple profile where all tiers have the same effects.
    /// </summary>
    public static OutcomeProfile Uniform(IReadOnlyList<IEffect> effects) =>
        new(effects, effects, effects);

    /// <summary>
    /// A neutral profile with no effects.
    /// </summary>
    public static OutcomeProfile Neutral => new(
        Array.Empty<IEffect>(),
        Array.Empty<IEffect>(),
        Array.Empty<IEffect>());
}
