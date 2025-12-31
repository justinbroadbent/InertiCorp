using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Content;

/// <summary>
/// Maps cards to their possible situation triggers.
/// </summary>
public static class CardSituationMappings
{
    /// <summary>
    /// All card to situation mappings.
    /// </summary>
    public static IReadOnlyDictionary<string, CardSituations> All => _mappings;

    private static readonly Dictionary<string, CardSituations> _mappings = new()
    {
        // === HIGH IMPACT CARDS ===
        ["PROJ_LAYOFFS"] = new CardSituations("PROJ_LAYOFFS", new[]
        {
            new SituationTrigger(SituationContent.SIT_UNION_ORGANIZING, OutcomeTier.Bad, TriggerWeight: 6),
            new SituationTrigger(SituationContent.SIT_KEY_PERFORMER_QUITS, OutcomeTier.Expected, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_KEY_PERFORMER_QUITS, OutcomeTier.Bad, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Expected, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 3),
        }),

        ["PROJ_DATA_MONETIZE"] = new CardSituations("PROJ_DATA_MONETIZE", new[]
        {
            new SituationTrigger(SituationContent.SIT_PRIVACY_INVESTIGATION, OutcomeTier.Bad, TriggerWeight: 6),
            new SituationTrigger(SituationContent.SIT_PRIVACY_INVESTIGATION, OutcomeTier.Expected, TriggerWeight: 3),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 3),
        }),

        ["PROJ_OFFSHORE"] = new CardSituations("PROJ_OFFSHORE", new[]
        {
            new SituationTrigger(SituationContent.SIT_KEY_PERFORMER_QUITS, OutcomeTier.Bad, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_MASS_RESIGNATION, OutcomeTier.Bad, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Expected, TriggerWeight: 4),
        }),

        ["PROJ_RTO_MANDATE"] = new CardSituations("PROJ_RTO_MANDATE", new[]
        {
            new SituationTrigger(SituationContent.SIT_MASS_RESIGNATION, OutcomeTier.Bad, TriggerWeight: 6),
            new SituationTrigger(SituationContent.SIT_KEY_PERFORMER_QUITS, OutcomeTier.Expected, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_KEY_PERFORMER_QUITS, OutcomeTier.Bad, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Expected, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 4),
        }),

        // === TECH CARDS ===
        ["PROJ_CLOUD_MIGRATION"] = new CardSituations("PROJ_CLOUD_MIGRATION", new[]
        {
            new SituationTrigger(SituationContent.SIT_TECH_PRESS_RECOGNITION, OutcomeTier.Good, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_DATA_MIGRATION_DISASTER, OutcomeTier.Bad, TriggerWeight: 6),
            new SituationTrigger(SituationContent.SIT_SECURITY_VULNERABILITY, OutcomeTier.Bad, TriggerWeight: 4),
        }),

        ["PROJ_DATA_LAKE"] = new CardSituations("PROJ_DATA_LAKE", new[]
        {
            new SituationTrigger(SituationContent.SIT_DATA_MIGRATION_DISASTER, OutcomeTier.Bad, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_SECURITY_VULNERABILITY, OutcomeTier.Bad, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_PRIVACY_INVESTIGATION, OutcomeTier.Bad, TriggerWeight: 3),
        }),

        ["PROJ_ZERO_TRUST"] = new CardSituations("PROJ_ZERO_TRUST", new[]
        {
            new SituationTrigger(SituationContent.SIT_SECURITY_VULNERABILITY, OutcomeTier.Good, TriggerWeight: 3),
            new SituationTrigger(SituationContent.SIT_TECH_PRESS_RECOGNITION, OutcomeTier.Good, TriggerWeight: 3),
        }),

        // === CULTURE CARDS ===
        ["PROJ_TOWN_HALL"] = new CardSituations("PROJ_TOWN_HALL", new[]
        {
            new SituationTrigger(SituationContent.SIT_EMPLOYEE_ENGAGEMENT_BOOST, OutcomeTier.Good, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 3),
        }),

        ["PROJ_CULTURE"] = new CardSituations("PROJ_CULTURE", new[]
        {
            new SituationTrigger(SituationContent.SIT_EMPLOYEE_ENGAGEMENT_BOOST, OutcomeTier.Good, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 2),
        }),

        ["PROJ_DEI"] = new CardSituations("PROJ_DEI", new[]
        {
            new SituationTrigger(SituationContent.SIT_EMPLOYEE_ENGAGEMENT_BOOST, OutcomeTier.Good, TriggerWeight: 5),
            new SituationTrigger(SituationContent.SIT_TECH_PRESS_RECOGNITION, OutcomeTier.Good, TriggerWeight: 4),
            new SituationTrigger(SituationContent.SIT_GLASSDOOR_FIRESTORM, OutcomeTier.Bad, TriggerWeight: 3),
        }),
    };

    /// <summary>
    /// Gets the situation mappings for a card.
    /// </summary>
    public static CardSituations? GetMappings(string cardId) =>
        _mappings.TryGetValue(cardId, out var mappings) ? mappings : null;

    /// <summary>
    /// Gets all card IDs that have situation mappings.
    /// </summary>
    public static IReadOnlyList<string> MappedCardIds => _mappings.Keys.ToList();
}
