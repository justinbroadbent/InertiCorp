namespace InertiCorp.Core.Content;

/// <summary>
/// Normal event cards - standard choices with trade-offs.
/// </summary>
public static class NormalEvents
{
    /// <summary>
    /// The Build Pipeline Ritual - tensions between speed, governance, and modernization.
    /// </summary>
    public static EventCard BuildPipelineRitual { get; } = new EventCard(
        "EVT_NORMAL_001_BUILD_PIPELINE_RITUAL",
        "The Build Pipeline Ritual",
        "The build pipeline is flaky again. Teams are debating whether to quick-fix, standardize, or replace it entirely.",
        new List<Choice>
        {
            new("CHC_QUICK_FIX_HACK", "Quick fix it", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 10),
                new MeterEffect(Meter.Runway, -5),
                new MeterEffect(Meter.Governance, -5)
            }),
            new("CHC_STANDARDIZE_GATES", "Standardize the gates", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 10),
                new MeterEffect(Meter.Alignment, 5),
                new MeterEffect(Meter.Delivery, -5)
            }),
            new("CHC_TOOL_REPLACEMENT", "Replace the tooling", new IEffect[]
            {
                new MeterEffect(Meter.Alignment, -10),
                new MeterEffect(Meter.Runway, -10),
                new MeterEffect(Meter.Governance, 5),
                new MeterEffect(Meter.Delivery, 5)
            })
        }
    );

    /// <summary>
    /// Alignment All-Hands - balancing communication overhead with morale.
    /// </summary>
    public static EventCard AlignmentAllHands { get; } = new EventCard(
        "EVT_NORMAL_002_ALIGNMENT_ALL_HANDS",
        "Alignment All-Hands",
        "It's time for the quarterly all-hands. Executives want alignment, but the team is exhausted from meetings.",
        new List<Choice>
        {
            new("CHC_HOST_MANDATORY", "Host mandatory all-hands", new IEffect[]
            {
                new MeterEffect(Meter.Alignment, 15),
                new MeterEffect(Meter.Morale, -10)
            }),
            new("CHC_SKIP_QUARTERLY", "Skip this quarter", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 5),
                new MeterEffect(Meter.Alignment, -10),
                new MeterEffect(Meter.Governance, -5)
            }),
            new("CHC_ASYNC_UPDATES", "Send async updates instead", new IEffect[]
            {
                new MeterEffect(Meter.Runway, 5),
                new MeterEffect(Meter.Morale, 5),
                new MeterEffect(Meter.Alignment, -5)
            })
        }
    );

    /// <summary>
    /// Cross-Team Dependency - dealing with blockers from other teams.
    /// </summary>
    public static EventCard CrossTeamDependency { get; } = new EventCard(
        "EVT_NORMAL_003_CROSS_TEAM_DEPENDENCY",
        "Cross-Team Dependency",
        "Your project is blocked waiting on another team's API. They say it's not a priority for them.",
        new List<Choice>
        {
            new("CHC_OWN_BUILD", "Build it yourself", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 10),
                new MeterEffect(Meter.Runway, -15),
                new MeterEffect(Meter.Governance, -5)
            }),
            new("CHC_NEGOTIATE_PRIORITY", "Negotiate priority", new IEffect[]
            {
                new MeterEffect(Meter.Alignment, 10),
                new MeterEffect(Meter.Delivery, -5)
            }),
            new("CHC_EXECUTIVE_ESCALATION", "Escalate to executives", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 10),
                new MeterEffect(Meter.Morale, -10)
            })
        }
    );

    /// <summary>
    /// Security Review Surprise - unplanned compliance work.
    /// </summary>
    public static EventCard SecurityReviewSurprise { get; } = new EventCard(
        "EVT_NORMAL_004_SECURITY_REVIEW_SURPRISE",
        "Security Review Surprise",
        "A mandatory security review has been announced. Your systems need assessment before the deadline.",
        new List<Choice>
        {
            new("CHC_DROP_EVERYTHING", "Drop everything for review", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 15),
                new MeterEffect(Meter.Delivery, -10),
                new MeterEffect(Meter.Morale, -5)
            }),
            new("CHC_MINIMAL_EFFORT", "Minimal effort compliance", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 5),
                new MeterEffect(Meter.Governance, -10)
            }),
            new("CHC_HIRE_CONSULTANTS", "Hire security consultants", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 10),
                new MeterEffect(Meter.Runway, -15)
            })
        }
    );

    /// <summary>
    /// Reorg Rumors - organizational uncertainty.
    /// </summary>
    public static EventCard ReorgRumors { get; } = new EventCard(
        "EVT_NORMAL_005_REORG_RUMORS",
        "Reorg Rumors",
        "Rumors of a major reorganization are circulating. Team members are anxious about their positions.",
        new List<Choice>
        {
            new("CHC_ADDRESS_TRANSPARENTLY", "Address it transparently", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 10),
                new MeterEffect(Meter.Alignment, 5),
                new MeterEffect(Meter.Governance, -5)
            }),
            new("CHC_STAY_QUIET", "Stay quiet", Array.Empty<IEffect>()),
            new("CHC_PREPARE_PITCH", "Prepare your pitch", new IEffect[]
            {
                new MeterEffect(Meter.Runway, -5),
                new MeterEffect(Meter.Alignment, 10)
            })
        }
    );

    /// <summary>
    /// Incident Weekend - production crisis management.
    /// </summary>
    public static EventCard IncidentWeekend { get; } = new EventCard(
        "EVT_NORMAL_006_INCIDENT_WEEKEND",
        "Incident Weekend",
        "A critical production incident occurred over the weekend. The team is waiting for direction.",
        new List<Choice>
        {
            new("CHC_ALL_HANDS_ON_DECK", "All hands on deck", new IEffect[]
            {
                new MeterEffect(Meter.Delivery, 15),
                new MeterEffect(Meter.Morale, -15)
            }),
            new("CHC_FOLLOW_PLAYBOOK", "Follow the playbook", new IEffect[]
            {
                new MeterEffect(Meter.Governance, 10),
                new MeterEffect(Meter.Delivery, 5),
                new MeterEffect(Meter.Morale, -5)
            }),
            new("CHC_WAIT_AND_SEE", "Wait and see", new IEffect[]
            {
                new MeterEffect(Meter.Morale, 5),
                new MeterEffect(Meter.Delivery, -10),
                new MeterEffect(Meter.Governance, -5)
            })
        }
    );

    /// <summary>
    /// All normal events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        BuildPipelineRitual,
        AlignmentAllHands,
        CrossTeamDependency,
        SecurityReviewSurprise,
        ReorgRumors,
        IncidentWeekend
    };
}
