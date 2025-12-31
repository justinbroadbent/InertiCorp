namespace InertiCorp.Core;

/// <summary>
/// A choice the player can make in response to an event.
/// Supports tiered outcomes (Good/Expected/Bad) via OutcomeProfile.
/// Some choices cost PC to select (representing using political capital to fix issues).
/// </summary>
public sealed record Choice(
    string ChoiceId,
    string Label,
    IReadOnlyList<IEffect> Effects,
    OutcomeProfile? OutcomeProfile = null,
    int CorporateIntensityDelta = 0,
    int PCCost = 0)
{
    /// <summary>
    /// Whether this choice has tiered outcomes (vs flat effects).
    /// </summary>
    public bool HasTieredOutcomes => OutcomeProfile is not null;

    /// <summary>
    /// Whether this is a "corporate/evil" choice that affects EvilScore.
    /// </summary>
    public bool IsCorporateChoice => CorporateIntensityDelta > 0;

    /// <summary>
    /// Whether this choice costs PC to select.
    /// </summary>
    public bool HasPCCost => PCCost > 0;

    /// <summary>
    /// Creates a choice with flat effects (no outcome variance).
    /// </summary>
    public static Choice Flat(string choiceId, string label, params IEffect[] effects) =>
        new(choiceId, label, effects.ToList());

    /// <summary>
    /// Creates a choice with tiered outcomes.
    /// </summary>
    public static Choice Tiered(
        string choiceId,
        string label,
        OutcomeProfile outcomeProfile,
        int corporateIntensityDelta = 0,
        int pcCost = 0) =>
        new(choiceId, label, Array.Empty<IEffect>(), outcomeProfile, corporateIntensityDelta, pcCost);

    /// <summary>
    /// Creates a corporate choice with tiered outcomes.
    /// </summary>
    public static Choice Corporate(
        string choiceId,
        string label,
        OutcomeProfile outcomeProfile,
        int corporateIntensityDelta = 1) =>
        new(choiceId, label, Array.Empty<IEffect>(), outcomeProfile, corporateIntensityDelta, 0);

    /// <summary>
    /// Creates a PC-cost choice that uses political capital to get better outcomes.
    /// </summary>
    public static Choice WithPCCost(
        string choiceId,
        string label,
        int pcCost,
        OutcomeProfile outcomeProfile) =>
        new(choiceId, label, Array.Empty<IEffect>(), outcomeProfile, 0, pcCost);
}
