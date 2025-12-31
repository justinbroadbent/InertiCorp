namespace InertiCorp.Core.Content;

/// <summary>
/// Placeholder event cards for UI development and testing.
/// </summary>
public static class PlaceholderEvents
{
    /// <summary>
    /// Meeting Overload event - choices affect Morale and Alignment.
    /// </summary>
    public static EventCard MeetingOverload { get; } = new EventCard(
        "EVT_PLACEHOLDER_001_MEETING_OVERLOAD",
        "Meeting Overload",
        "Your calendar is triple-booked with stakeholder syncs, retrospectives, and 'quick alignment chats'. What do you do?",
        new List<Choice>
        {
            new("CHC_MEETING_ATTEND_ALL", "Attend everything", new IEffect[]
            {
                new MeterEffect(Meter.Alignment, 10),
                new MeterEffect(Meter.Morale, -15)
            }),
            new("CHC_MEETING_DELEGATE", "Delegate to your team", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 5),
                new MeterEffect(Meter.Alignment, -10)
            }),
            new("CHC_MEETING_CANCEL", "Cancel the non-essential ones", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 10),
                new MeterEffect(Meter.Alignment, -5)
            })
        }
    );

    /// <summary>
    /// Deadline Pressure event - choices affect Delivery and Runway.
    /// </summary>
    public static EventCard DeadlinePressure { get; } = new EventCard(
        "EVT_PLACEHOLDER_002_DEADLINE_PRESSURE",
        "Deadline Pressure",
        "The client is demanding the feature ship by end of quarter. Your team says it's impossible without cutting corners.",
        new List<Choice>
        {
            new("CHC_DEADLINE_CRUNCH", "Push for crunch time", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 15),
                new MeterEffect(Meter.Morale, -10),
                new MeterEffect(Meter.Runway, -5)
            }),
            new("CHC_DEADLINE_NEGOTIATE", "Negotiate a scope reduction", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 5),
                new MeterEffect(Meter.Runway, 5)
            }),
            new("CHC_DEADLINE_DELAY", "Request a deadline extension", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, -5),
                new MeterEffect(Meter.Runway, -10)
            })
        }
    );

    /// <summary>
    /// All placeholder events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        MeetingOverload,
        DeadlinePressure
    };
}
