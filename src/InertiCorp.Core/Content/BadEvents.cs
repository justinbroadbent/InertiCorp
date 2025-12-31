namespace InertiCorp.Core.Content;

/// <summary>
/// Bad event cards - crises that require damage control.
/// </summary>
public static class BadEvents
{
    /// <summary>
    /// Audit Finding - compliance crisis requiring response.
    /// </summary>
    public static EventCard AuditFinding { get; } = new EventCard(
        "EVT_BAD_001_AUDIT_FINDING",
        "Audit Finding",
        "Internal audit found critical compliance gaps. They're demanding immediate action.",
        new List<Choice>
        {
            new("CHC_IMMEDIATE_REMEDIATION", "Immediate remediation", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 15),
                new MeterEffect(Meter.Delivery, -15),
                new MeterEffect(Meter.Morale, -10)
            }),
            new("CHC_DISPUTE_FINDINGS", "Dispute the findings", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 5),
                new MeterEffect(Meter.Governance, -10)
            }),
            new("CHC_RISK_ACCEPTANCE", "Accept the risk", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 5),
                new MeterEffect(Meter.Runway, -10),
                new MeterEffect(Meter.Governance, -15)
            })
        }
    );

    /// <summary>
    /// Key Person Quits - talent retention crisis.
    /// </summary>
    public static EventCard KeyPersonQuits { get; } = new EventCard(
        "EVT_BAD_002_KEY_PERSON_QUITS",
        "Key Person Quits",
        "Your most critical team member just handed in their resignation. They have unique knowledge.",
        new List<Choice>
        {
            new("CHC_RETENTION_BONUS", "Offer retention bonus", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 10),
                new MeterEffect(Meter.Runway, -20)
            }),
            new("CHC_REDISTRIBUTE_WORK", "Redistribute their work", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, -10),
                new MeterEffect(Meter.Morale, -10),
                new MeterEffect(Meter.Alignment, 5)
            }),
            new("CHC_BACKFILL_FAST", "Backfill immediately", new IEffect[]
            {
                new MeterEffect(Meter.Runway, -15),
                new MeterEffect(Meter.Delivery, -5),
                new MeterEffect(Meter.Governance, -5)
            })
        }
    );

    /// <summary>
    /// All bad events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        AuditFinding,
        KeyPersonQuits
    };
}
