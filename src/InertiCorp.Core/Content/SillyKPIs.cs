namespace InertiCorp.Core.Content;

/// <summary>
/// Silly corporate KPIs and news ticker events.
/// Inspired by SimCity's "Reticulating Splines" and other loading screen jokes.
/// </summary>
public static class SillyKPIs
{
    /// <summary>
    /// Meaningless corporate KPIs that sound important but aren't.
    /// </summary>
    public static readonly (string Name, string Unit)[] KPIDefinitions =
    {
        ("Synergy Index", "pts"),
        ("Disruption Coefficient", "%"),
        ("Innovation Velocity", "m/s"),
        ("Stakeholder Alignment Score", "σ"),
        ("Digital Transformation Index", "DTI"),
        ("Agility Quotient", "AQ"),
        ("Brand Resonance Factor", "BRF"),
        ("Customer Delight Metric", "☺"),
        ("Paradigm Shift Readiness", "PSR"),
        ("Core Competency Rating", "CCR"),
        ("Thought Leadership Index", "TLI"),
        ("Cross-Functional Synergy", "XFS"),
        ("Value Stream Velocity", "VSV"),
        ("Holistic Engagement Score", "HES"),
        ("Strategic Pivot Potential", "SPP"),
        ("Blue Sky Thinking Index", "BSTI"),
    };

    /// <summary>
    /// Silly news ticker events with their KPI effects.
    /// Format: (headline, kpiIndex, delta)
    /// </summary>
    public static readonly (string Headline, int KpiIndex, int Delta)[] TickerEvents =
    {
        // Synergy related (index 0)
        ("Cross-departmental synergy workshop achieves record attendance", 0, 3),
        ("Synergy Index dips after exec forgets to CC all on email", 0, -2),
        ("New synergy consultant hired to synergize synergies", 0, 5),
        ("Synergy levels normalize after ping pong table installed", 0, 2),

        // Disruption related (index 1)
        ("Company successfully disrupts own supply chain", 1, 4),
        ("Disruption Coefficient spikes following AI announcement", 1, 6),
        ("Industry experts question if we're disrupting enough", 1, -3),
        ("Disruption levels stable after pivoting to blockchain", 1, 2),

        // Innovation related (index 2)
        ("Innovation Velocity increases after removing approval process", 2, 5),
        ("Hackathon produces 47 ideas, 0 implementable", 2, -1),
        ("Innovation lab renamed to 'Innovation Experience Center'", 2, 3),
        ("Standing desks boost innovation by unverified amount", 2, 2),

        // Stakeholder related (index 3)
        ("Stakeholders aligned after 6-hour meeting", 3, 4),
        ("Key stakeholder requests additional stakeholder meeting", 3, -2),
        ("Stakeholder Alignment Score certified by external auditors", 3, 3),
        ("New stakeholder identified; alignment delayed", 3, -1),

        // Digital Transformation (index 4)
        ("Digital Transformation now 127% complete", 4, 8),
        ("Legacy system successfully migrated to newer legacy system", 4, 2),
        ("AI integration pending AI approval", 4, 3),
        ("Cloud migration achieves cloud-native cloudiness", 4, 4),

        // Agility (index 5)
        ("Agility Quotient surges after standup reduced to 45 minutes", 5, 3),
        ("Sprint retrospective determines sprints should be sprintier", 5, 2),
        ("Agile transformation enters fourth year of planning", 5, -2),
        ("Kanban board running low on sticky notes", 5, -1),

        // Brand (index 6)
        ("Brand Resonance peaks after logo font changed to sans-serif", 6, 4),
        ("Thought piece on thought leadership gains thought traction", 6, 3),
        ("Brand awareness up 12% in markets we don't operate in", 6, 2),
        ("Logo gradient adjustment improves brand perception", 6, 1),

        // Customer Delight (index 7)
        ("Customer Delight Metric reaches new high after survey redesign", 7, 5),
        ("Customers delighted by new delight measurement system", 7, 3),
        ("Delight levels normalize following price increase", 7, -4),
        ("New NPS survey confirms customers exist", 7, 2),

        // Paradigm (index 8)
        ("Paradigm shift successfully shifted paradigms", 8, 6),
        ("New paradigm discovered in Q3 earnings call", 8, 4),
        ("Paradigm Shift Readiness stable despite actual paradigm shift", 8, 2),
        ("Industry paradigm remains unshifted", 8, -1),

        // Core Competency (index 9)
        ("Core competencies reconfirmed as still core", 9, 2),
        ("New core competency added: identifying core competencies", 9, 3),
        ("Competency assessment reveals competent competencies", 9, 4),
        ("Core competency training completed by core team", 9, 2),

        // Thought Leadership (index 10)
        ("LinkedIn article establishes thought leadership credentials", 10, 3),
        ("Thought leader hosts webinar on hosting webinars", 10, 4),
        ("TED talk proposal rejected; TEDx talk approved", 10, 2),
        ("White paper published on importance of white papers", 10, 3),

        // Cross-Functional (index 11)
        ("Cross-functional tiger team assembled to study tiger teams", 11, 3),
        ("Silos successfully identified and labeled", 11, 2),
        ("Matrix organization adds fourth dimension", 11, 4),
        ("Cross-functional meeting scheduled to reduce meetings", 11, -1),

        // Value Stream (index 12)
        ("Value stream mapped using different colored markers", 12, 2),
        ("Value Stream Velocity accelerates after coffee upgrade", 12, 3),
        ("Lean workshop identifies 47 types of waste", 12, 4),
        ("Value-add ratio improves by removing value analysis", 12, 2),

        // Holistic Engagement (index 13)
        ("Holistic Engagement Score hits all-time high during pizza party", 13, 5),
        ("Employee engagement survey achieves 12% response rate", 13, 2),
        ("Engagement improves after word 'mandatory' removed from events", 13, 3),
        ("Town hall Q&A limited to pre-approved questions", 13, -2),

        // Strategic Pivot (index 14)
        ("Strategic pivot options expanded to include diagonal pivots", 14, 3),
        ("Company pivots to pivot-as-a-service model", 14, 5),
        ("Strategic Pivot Potential stable at maximum pivot readiness", 14, 2),
        ("Board approves exploratory pivot exploration", 14, 4),

        // Blue Sky (index 15)
        ("Blue Sky Thinking Index soars during offsite", 15, 6),
        ("Brainstorm session produces 200 ideas, 0 budgets", 15, 3),
        ("Innovation sandbox renamed to 'Innovation Playground'", 15, 2),
        ("Blue sky thinking temporarily grounded by reality", 15, -3),

        // Generic corporate nonsense
        ("Reticulating corporate splines...", 0, 1),
        ("Optimizing organizational osmosis...", 3, 2),
        ("Calibrating corporate compass...", 8, 1),
        ("Aligning strategic alignments...", 3, 3),
        ("Synergizing synergy matrices...", 0, 4),
        ("Leveraging leverage opportunities...", 14, 2),
        ("Actualizing actualization potential...", 8, 1),
        ("Ideating ideation frameworks...", 2, 2),
        ("Disrupting disruption paradigms...", 1, 3),
        ("Transforming transformation initiatives...", 4, 2),

        // Additional corporate news events
        ("CEO completes mindfulness retreat; returns more aggressive than ever", 13, -3),
        ("Quarterly town hall postponed to avoid uncomfortable questions", 13, -2),
        ("Anonymous employee feedback reveals employees want higher pay", 13, 1),
        ("Company values poster successfully replaced with newer poster", 3, 2),
        ("Mandatory fun committee announces mandatory team building event", 13, -4),
        ("IT helpdesk achieves personal best: 47-minute hold time", 7, -3),
        ("Supply chain issues blamed on everything except supply chain", 1, -2),
        ("Marketing pivots strategy for the third time this quarter", 14, 3),
        ("Consultant engagement extended 'just a few more weeks'", 0, 2),
        ("Employee suggestion box installed; key immediately lost", 13, -1),
        ("Work-life balance initiative cancelled due to workload", 13, -5),
        ("Open office plan hailed as success by executives with offices", 9, -2),
        ("Diversity hire promoted to head of Diversity and nothing else", 3, 1),
        ("All-hands meeting runs 45 minutes over; no questions answered", 3, -2),
        ("Competitive analysis reveals competitors also confused", 14, 2),
        ("Customer feedback loop successfully closed; feedback discarded", 7, -3),
        ("Middle management expands to manage the managers of managers", 11, 3),
        ("Zero inbox policy announced; inbox count unchanged", 12, 0),
        ("Expense reports now require 7 approvals for coffee", 9, 2),
        ("Best employer award purchased from award-selling company", 6, 4),
    };

    /// <summary>
    /// Generates a random set of KPI values for a new game.
    /// </summary>
    public static Dictionary<int, int> GenerateInitialKPIs(IRng rng)
    {
        var kpis = new Dictionary<int, int>();
        for (int i = 0; i < KPIDefinitions.Length; i++)
        {
            kpis[i] = rng.NextInt(40, 80); // Start with values between 40-80
        }
        return kpis;
    }

    /// <summary>
    /// Gets a random ticker event.
    /// </summary>
    public static (string Headline, int KpiIndex, int Delta) GetRandomEvent(IRng rng)
    {
        var index = rng.NextInt(0, TickerEvents.Length);
        return TickerEvents[index];
    }

    /// <summary>
    /// Gets the KPI name and unit by index.
    /// </summary>
    public static (string Name, string Unit) GetKPI(int index)
    {
        if (index < 0 || index >= KPIDefinitions.Length)
            return ("Unknown Metric", "?");
        return KPIDefinitions[index];
    }
}
