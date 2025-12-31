using InertiCorp.Core.Crisis;

namespace InertiCorp.Core.Content;

/// <summary>
/// Content definitions for crises and response packages.
/// </summary>
public static class CrisisContent
{
    /// <summary>
    /// All available crisis definitions.
    /// </summary>
    public static IReadOnlyList<CrisisDefinition> AllCrises { get; } = new[]
    {
        new CrisisDefinition(
            CrisisId: "email_thread_meltdown",
            Title: "Email Thread Meltdown",
            Description: "A 47-person reply-all chain has devolved into open warfare between Engineering and Product.",
            Severity: 3,
            Tags: new[] { "miscommunication", "internal" },
            DeadlineAfterTurns: 2,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Alignment, -6 },
                { Meter.Morale, -4 }
            },
            OngoingImpact: new Dictionary<Meter, int>
            {
                { Meter.Delivery, -1 }
            }),

        new CrisisDefinition(
            CrisisId: "competitor_preannouncement",
            Title: "Competitor Pre-Announcement",
            Description: "Your main competitor just announced they're launching the feature you've been working on for 8 months.",
            Severity: 4,
            Tags: new[] { "competitor", "market" },
            DeadlineAfterTurns: 1,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Runway, -4 },
                { Meter.Delivery, -3 }
            },
            MinimumSpendToFullyMitigate: 3),

        new CrisisDefinition(
            CrisisId: "key_engineer_resignation",
            Title: "Key Engineer Resignation",
            Description: "The only person who understands the payment system just handed in their notice.",
            Severity: 4,
            Tags: new[] { "personnel", "technical" },
            DeadlineAfterTurns: 2,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Delivery, -8 },
                { Meter.Governance, -3 }
            },
            OngoingImpact: new Dictionary<Meter, int>
            {
                { Meter.Morale, -1 }
            },
            MinimumSpendToFullyMitigate: 4),

        new CrisisDefinition(
            CrisisId: "compliance_audit_surprise",
            Title: "Surprise Compliance Audit",
            Description: "Regulators have shown up unannounced. Your documentation is... aspirational.",
            Severity: 5,
            Tags: new[] { "regulatory", "governance" },
            DeadlineAfterTurns: 1,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Governance, -10 },
                { Meter.Runway, -5 },
                { Meter.Alignment, -5 }
            },
            MinimumSpendToFullyMitigate: 5),

        new CrisisDefinition(
            CrisisId: "production_outage",
            Title: "Production Outage",
            Description: "The site is down. Customers are tweeting. The on-call engineer is on a flight to Hawaii.",
            Severity: 4,
            Tags: new[] { "technical", "customer" },
            DeadlineAfterTurns: 1,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Delivery, -6 },
                { Meter.Runway, -3 },
                { Meter.Alignment, -4 }
            }),

        new CrisisDefinition(
            CrisisId: "board_leak",
            Title: "Board Meeting Leak",
            Description: "Someone leaked the Q3 projections to TechCrunch. The board is not amused.",
            Severity: 3,
            Tags: new[] { "internal", "governance" },
            DeadlineAfterTurns: 2,
            BaseImpact: new Dictionary<Meter, int>
            {
                { Meter.Alignment, -8 },
                { Meter.Governance, -4 }
            })
    };

    /// <summary>
    /// All available response packages.
    /// </summary>
    public static IReadOnlyList<CrisisResponse> AllResponses { get; } = new[]
    {
        // Budget tier responses (cheap, risky)
        new CrisisResponse(
            ResponseId: "assign_pm_budget",
            Title: "Assign a Project Manager (Budget)",
            Description: "Throw an available PM at the problem. They mean well.",
            CostPC: 1,
            MitigationBonus: 0,
            StaffQuality: StaffQualityWeights.Budget,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Governance, 1 } }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 1),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Escalate,
                    ScheduleAftershocks: new[] { "stakeholder_whiplash" }))),

        new CrisisResponse(
            ResponseId: "damage_control_memo",
            Title: "Send a Damage Control Memo",
            Description: "Draft a carefully-worded email. Hope it works.",
            CostPC: 1,
            MitigationBonus: -1,
            StaffQuality: StaffQualityWeights.Budget,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ExtendDeadline,
                    DeadlineExtension: 1),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.None,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Alignment, -2 } }))),

        // Standard tier responses
        new CrisisResponse(
            ResponseId: "assign_pm_standard",
            Title: "Assign a Competent PM",
            Description: "Actually vet the PM first this time.",
            CostPC: 2,
            MitigationBonus: 1,
            StaffQuality: StaffQualityWeights.Standard,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Governance, 2 } }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 2),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 1))),

        new CrisisResponse(
            ResponseId: "executive_attention",
            Title: "Direct Executive Attention",
            Description: "Clear your calendar and handle this personally.",
            CostPC: 3,
            MitigationBonus: 1,
            StaffQuality: StaffQualityWeights.Standard,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int>
                    {
                        { Meter.Alignment, 2 },
                        { Meter.Morale, 1 }
                    }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Runway, -1 } }),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 1,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Alignment, -1 } }))),

        // Premium tier responses
        new CrisisResponse(
            ResponseId: "tiger_team",
            Title: "Spin Up a Tiger Team",
            Description: "Assemble your best people. Cancel their vacations.",
            CostPC: 4,
            MitigationBonus: 2,
            StaffQuality: StaffQualityWeights.Premium,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Delivery, 2 } },
                    SpawnEffects: new[] { "tiger_team" }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Runway, -1 } }),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 2,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Runway, -2 } }))),

        new CrisisResponse(
            ResponseId: "consultant_cavalry",
            Title: "Call in the Consultants",
            Description: "Expensive, but they come with impressive slide decks.",
            CostPC: 5,
            MitigationBonus: 3,
            StaffQuality: StaffQualityWeights.Premium,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int>
                    {
                        { Meter.Governance, 3 },
                        { Meter.Runway, -2 }
                    }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Runway, -3 } }),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 1,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Runway, -4 } }))),

        // Specialized responses
        new CrisisResponse(
            ResponseId: "all_hands_meeting",
            Title: "Emergency All-Hands Meeting",
            Description: "Address the crisis directly with the whole company.",
            CostPC: 2,
            MitigationBonus: 1,
            StaffQuality: StaffQualityWeights.Standard,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int>
                    {
                        { Meter.Morale, 3 },
                        { Meter.Alignment, 1 }
                    }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 1,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Morale, 1 } }),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.None,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Morale, -2 } })))
        {
            EffectiveTags = new[] { "internal", "personnel", "miscommunication" }
        },

        new CrisisResponse(
            ResponseId: "technical_deep_dive",
            Title: "Technical Deep Dive",
            Description: "Lock the engineers in a room until they fix it.",
            CostPC: 3,
            MitigationBonus: 2,
            StaffQuality: StaffQualityWeights.Standard,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(
                    CrisisOp: CrisisOperation.Mitigate,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Delivery, 2 } }),
                Mixed: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ReduceSeverity,
                    SeverityReduction: 2,
                    MeterDeltas: new Dictionary<Meter, int> { { Meter.Morale, -1 } }),
                Fail: new ResponseOutcome(
                    CrisisOp: CrisisOperation.ExtendDeadline,
                    DeadlineExtension: 1)))
        {
            EffectiveTags = new[] { "technical" }
        }
    };

    /// <summary>
    /// Gets a crisis definition by ID.
    /// </summary>
    public static CrisisDefinition? GetCrisis(string crisisId) =>
        AllCrises.FirstOrDefault(c => c.CrisisId == crisisId);

    /// <summary>
    /// Gets a response package by ID.
    /// </summary>
    public static CrisisResponse? GetResponse(string responseId) =>
        AllResponses.FirstOrDefault(r => r.ResponseId == responseId);

    /// <summary>
    /// Gets all response packages that the player can currently afford.
    /// </summary>
    public static IReadOnlyList<CrisisResponse> GetAffordableResponses(int politicalCapital) =>
        AllResponses.Where(r => r.CostPC <= politicalCapital).ToList();

    /// <summary>
    /// Gets responses that are effective for a given crisis (matching tags).
    /// </summary>
    public static IReadOnlyList<CrisisResponse> GetEffectiveResponses(CrisisInstance crisis) =>
        AllResponses.Where(r =>
            r.EffectiveTags.Any(t => crisis.Tags.Contains(t))).ToList();
}
