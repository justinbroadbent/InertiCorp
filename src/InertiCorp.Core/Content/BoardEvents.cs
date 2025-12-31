namespace InertiCorp.Core.Content;

/// <summary>
/// Board deck cards - directives and mandates from leadership.
/// These are drawn during the Board phase of each quarter.
/// </summary>
public static class BoardEvents
{
    /// <summary>
    /// Cost Reduction Mandate - board demands cuts.
    /// </summary>
    public static EventCard CostReductionMandate { get; } = new EventCard(
        "BOARD_001_COST_REDUCTION",
        "Cost Reduction Mandate",
        "The board is demanding a 15% cost reduction this quarter. How will you comply?",
        new List<Choice>
        {
            Choice.Tiered("CHC_LAYOFFS", "Implement layoffs",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 25) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, 20), new MeterEffect(Meter.Morale, -20) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Morale, -30), new MeterEffect(Meter.Delivery, -15) }
                )),
            Choice.Tiered("CHC_CUT_TOOLS", "Cut tools and perks",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Morale, -5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) }
                )),
            Choice.Corporate("CHC_CREATIVE_ACCOUNTING", "Creative accounting",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 20) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Governance, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -30), new MeterEffect(Meter.Runway, -10) }
                ), corporateIntensityDelta: 3)
        }
    );

    /// <summary>
    /// Growth Target - aggressive expansion demands.
    /// </summary>
    public static EventCard GrowthTarget { get; } = new EventCard(
        "BOARD_002_GROWTH_TARGET",
        "Growth Target",
        "The board expects 30% growth next quarter. They want to see your strategy.",
        new List<Choice>
        {
            Choice.Tiered("CHC_AGGRESSIVE_HIRING", "Aggressive hiring spree",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Runway, -15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Governance, -15) }
                )),
            Choice.Tiered("CHC_SUSTAINABLE_GROWTH", "Propose sustainable growth",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Delivery, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_PROMISE_ANYTHING", "Promise whatever they want",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, 20), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    /// <summary>
    /// Competitive Pressure - board worried about competitors.
    /// </summary>
    public static EventCard CompetitivePressure { get; } = new EventCard(
        "BOARD_003_COMPETITION",
        "Competitive Pressure",
        "A competitor just launched a major product. The board is nervous.",
        new List<Choice>
        {
            Choice.Tiered("CHC_ACCELERATE_ROADMAP", "Accelerate the roadmap",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Morale, -5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Governance, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Governance, -15) }
                )),
            Choice.Tiered("CHC_STAY_THE_COURSE", "Stay the course",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -5) }
                )),
            Choice.Corporate("CHC_ACQUIRE_COMPETITOR", "Propose acquiring them",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Alignment, 10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -30), new MeterEffect(Meter.Governance, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    /// <summary>
    /// All board events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        CostReductionMandate,
        GrowthTarget,
        CompetitivePressure
    };
}
