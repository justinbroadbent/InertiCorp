namespace InertiCorp.Core.Content;

/// <summary>
/// Good event cards - positive opportunities with choices.
/// </summary>
public static class GoodEvents
{
    /// <summary>
    /// Surprise Budget - unexpected resources to allocate.
    /// </summary>
    public static EventCard SurpriseBudget { get; } = new EventCard(
        "EVT_GOOD_001_SURPRISE_BUDGET",
        "Surprise Budget",
        "Finance found unallocated budget that expires this quarter. How do you spend it?",
        new List<Choice>
        {
            new("CHC_HIRE_CONTRACTORS", "Hire contractors", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 15),
                new MeterEffect(Meter.Governance, -5)
            }),
            new("CHC_PAY_DOWN_DEBT", "Pay down tech debt", new IEffect[]
            {
                new MeterEffect(Meter.Runway, 20),
                new MeterEffect(Meter.Delivery, -5)
            }),
            new("CHC_TEAM_OFFSITE", "Team offsite", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 15),
                new MeterEffect(Meter.Alignment, 10),
                new MeterEffect(Meter.Runway, -10)
            })
        }
    );

    /// <summary>
    /// Executive Attention - high-level visibility opportunity.
    /// </summary>
    public static EventCard ExecutiveAttention { get; } = new EventCard(
        "EVT_GOOD_002_EXECUTIVE_ATTENTION",
        "Executive Attention",
        "An executive has taken interest in your project. They're offering sponsorship and visibility.",
        new List<Choice>
        {
            new("CHC_ACCEPT_SPONSORSHIP", "Accept sponsorship", new IEffect[]
            {
                new MeterEffect(Meter.Alignment, 15),
                new MeterEffect(Meter.Governance, 10),
                new MeterEffect(Meter.Morale, -5)
            }),
            new("CHC_POLITELY_DECLINE", "Politely decline", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 10),
                new MeterEffect(Meter.Runway, 5),
                new MeterEffect(Meter.Alignment, -5)
            }),
            new("CHC_NEGOTIATE_RESOURCES", "Negotiate for resources", new IEffect[]
            {
                new MeterEffect(Meter.Runway, 15),
                new MeterEffect(Meter.Delivery, 5),
                new MeterEffect(Meter.Governance, -5)
            })
        }
    );

    /// <summary>
    /// All good events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        SurpriseBudget,
        ExecutiveAttention
    };
}
