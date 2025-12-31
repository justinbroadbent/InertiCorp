namespace InertiCorp.Core.Content;

/// <summary>
/// Project deck cards - initiatives and opportunities.
/// These are drawn during the Project phase of each quarter.
/// </summary>
public static class ProjectEvents
{
    /// <summary>
    /// Surprise Budget - unexpected resources to allocate.
    /// </summary>
    public static EventCard SurpriseBudget { get; } = new EventCard(
        "PROJECT_001_SURPRISE_BUDGET",
        "Surprise Budget",
        "Finance found unallocated budget that expires this quarter. How do you spend it?",
        new List<Choice>
        {
            Choice.Tiered("CHC_HIRE_CONTRACTORS", "Hire contractors",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Governance, -5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Governance, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Governance, -10) }
                )),
            Choice.Tiered("CHC_PAY_DOWN_DEBT", "Pay down tech debt",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 25), new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, 20), new MeterEffect(Meter.Delivery, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Delivery, -10) }
                )),
            Choice.Tiered("CHC_TEAM_OFFSITE", "Team offsite",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 20), new MeterEffect(Meter.Alignment, 15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -15) }
                ))
        }
    );

    /// <summary>
    /// New Product Initiative - strategic opportunity.
    /// </summary>
    public static EventCard NewProductInitiative { get; } = new EventCard(
        "PROJECT_002_NEW_PRODUCT",
        "New Product Initiative",
        "There's an opportunity to spin up a new product line. It could be transformative.",
        new List<Choice>
        {
            Choice.Tiered("CHC_LEAD_INITIATIVE", "Lead the initiative",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Alignment, 15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Morale, -10) }
                )),
            Choice.Tiered("CHC_SUPPORT_ROLE", "Take a support role",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5) }
                )),
            Choice.Corporate("CHC_CLAIM_CREDIT", "Position to claim credit later",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Governance, -10) }
                ), corporateIntensityDelta: 1)
        }
    );

    /// <summary>
    /// Process Improvement - operational efficiency opportunity.
    /// </summary>
    public static EventCard ProcessImprovement { get; } = new EventCard(
        "PROJECT_003_PROCESS",
        "Process Improvement",
        "The team has identified bottlenecks that could be streamlined with investment.",
        new List<Choice>
        {
            Choice.Tiered("CHC_AUTOMATE", "Invest in automation",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Morale, -10) }
                )),
            Choice.Tiered("CHC_HIRE_MORE", "Hire more people",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Runway, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, -10) }
                )),
            Choice.Corporate("CHC_OUTSOURCE", "Outsource to cut costs",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 20) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    /// <summary>
    /// All project events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        SurpriseBudget,
        NewProductInitiative,
        ProcessImprovement
    };
}
