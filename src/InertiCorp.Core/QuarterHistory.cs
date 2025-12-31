namespace InertiCorp.Core;

/// <summary>
/// Tracks historical data for each completed quarter.
/// Used for generating corporate reports and charts.
/// </summary>
public sealed record QuarterSnapshot(
    int QuarterNumber,
    int Profit,
    int TotalProfit,
    int BoardFavorability,
    int EvilScore,
    int AccumulatedBonus,
    int Delivery,
    int Morale,
    int Governance,
    int Alignment,
    int Runway,
    bool DirectiveMet,
    int CardsPlayed);

/// <summary>
/// Maintains a history of quarter snapshots for reporting.
/// </summary>
public sealed class QuarterHistory
{
    private readonly List<QuarterSnapshot> _snapshots = new();

    public IReadOnlyList<QuarterSnapshot> Snapshots => _snapshots;

    public void AddSnapshot(QuarterSnapshot snapshot)
    {
        _snapshots.Add(snapshot);
    }

    public static QuarterSnapshot CreateSnapshot(
        int quarterNumber,
        CEOState ceo,
        OrgState org,
        bool directiveMet,
        int cardsPlayed)
    {
        return new QuarterSnapshot(
            QuarterNumber: quarterNumber,
            Profit: ceo.LastQuarterProfit,  // Total profit from last quarter (base ops + projects)
            TotalProfit: ceo.TotalProfit,
            BoardFavorability: ceo.BoardFavorability,
            EvilScore: ceo.EvilScore,
            AccumulatedBonus: ceo.AccumulatedBonus,
            Delivery: org.Delivery,
            Morale: org.Morale,
            Governance: org.Governance,
            Alignment: org.Alignment,
            Runway: org.Runway,
            DirectiveMet: directiveMet,
            CardsPlayed: cardsPlayed);
    }

    /// <summary>
    /// Generates silly "time allocation" data for pie chart.
    /// The percentages are deterministic based on quarters survived for consistency.
    /// </summary>
    public static IReadOnlyList<(string Activity, float Percentage)> GetTimeAllocation(int quartersSurvived)
    {
        // Base allocations that shift slightly over time
        var seed = quartersSurvived * 7;
        var activities = new List<(string, float)>
        {
            ("Meetings about meetings", 18 + (seed % 5)),
            ("Strategic alignment sessions", 14 + ((seed + 1) % 4)),
            ("Synergy cultivation", 12 + ((seed + 2) % 3)),
            ("Email management", 11 + ((seed + 3) % 4)),
            ("Coffee breaks (networking)", 9 + ((seed + 4) % 3)),
            ("Actual productive work", 8 - ((seed + 5) % 3)),
            ("LinkedIn thought leadership", 7 + ((seed + 6) % 2)),
            ("Budget justification", 6 + ((seed + 7) % 3)),
            ("Town halls & all-hands", 5 + ((seed + 8) % 2)),
            ("Other", 10 - ((seed + 9) % 4))
        };

        // Normalize to 100%
        var total = activities.Sum(a => a.Item2);
        return activities.Select(a => (a.Item1, a.Item2 / total * 100)).ToList();
    }

    /// <summary>
    /// Generates silly KPI data based on actual game metrics.
    /// </summary>
    public static IReadOnlyList<(string Kpi, float Value, string Unit)> GetKPIs(
        CEOState ceo, OrgState org, int cardsPlayedTotal)
    {
        return new List<(string, float, string)>
        {
            ("Synergy Index", org.Alignment * 1.3f + org.Morale * 0.7f, "SU"),
            ("Innovation Velocity", (org.Delivery + cardsPlayedTotal * 2) / 10f, "IV/Q"),
            ("Stakeholder Satisfaction", ceo.BoardFavorability * 0.9f + 10, "%"),
            ("Operational Excellence", (org.Governance + org.Runway) / 2f, "OEI"),
            ("Culture Index", org.Morale - ceo.EvilScore * 2 + 50, "CI"),
            ("Runway Efficiency Ratio", org.Runway * 1.2f, "RER"),
            ("Leadership Presence Score", Math.Min(100, ceo.QuartersSurvived * 8 + 20), "LPS"),
            ("Digital Transformation Progress", Math.Min(95, cardsPlayedTotal * 3 + 15), "%")
        };
    }

    /// <summary>
    /// Generates corporate buzzword for chart titles.
    /// </summary>
    public static string GetBuzzwordTitle(int seed)
    {
        var adjectives = new[] { "Strategic", "Synergistic", "Holistic", "Agile", "Disruptive", "Innovative", "Transformational", "Data-Driven" };
        var nouns = new[] { "Performance", "Alignment", "Excellence", "Growth", "Optimization", "Transformation", "Value Creation", "Outcomes" };
        var suffixes = new[] { "Dashboard", "Overview", "Analysis", "Deep Dive", "Retrospective", "Summary", "Report" };

        return $"{adjectives[seed % adjectives.Length]} {nouns[(seed / 8) % nouns.Length]} {suffixes[(seed / 64) % suffixes.Length]}";
    }
}
