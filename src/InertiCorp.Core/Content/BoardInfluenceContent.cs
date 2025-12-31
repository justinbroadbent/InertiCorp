using InertiCorp.Core.Quarter;

namespace InertiCorp.Core.Content;

/// <summary>
/// Content definitions for board influence packages.
/// </summary>
public static class BoardInfluenceContent
{
    /// <summary>
    /// All available board influence packages.
    /// </summary>
    public static IReadOnlyList<BoardInfluencePackage> AllPackages { get; } = new[]
    {
        new BoardInfluencePackage(
            PackageId: "spin_metrics",
            Title: "Spin the Metrics",
            Description: "Have IR craft a more favorable interpretation of this quarter's numbers. Cheap but risky.",
            CostPC: 2,
            ScoreBonus: 5,
            BacklashRiskPercent: 40,
            BacklashConsequenceId: "governance_scrutiny"),

        new BoardInfluencePackage(
            PackageId: "executive_offsite",
            Title: "Executive Offsite & Narrative Reset",
            Description: "Wine and dine the board at a Napa retreat. Reset the conversation on your terms.",
            CostPC: 4,
            ScoreBonus: 10,
            BacklashRiskPercent: 25,
            BacklashConsequenceId: "governance_scrutiny"),

        new BoardInfluencePackage(
            PackageId: "relationship_tour",
            Title: "Board Relationship Tour",
            Description: "Schedule 1:1s with each board member. Build rapport, share your vision, secure allies.",
            CostPC: 6,
            ScoreBonus: 15,
            BacklashRiskPercent: 15,
            BacklashConsequenceId: "alignment_erosion"),

        new BoardInfluencePackage(
            PackageId: "strategic_leak",
            Title: "Strategic Information Leak",
            Description: "Let slip some 'confidential' good news to sympathetic board members before the meeting.",
            CostPC: 3,
            ScoreBonus: 8,
            BacklashRiskPercent: 50,
            BacklashConsequenceId: "ethics_probe"),

        new BoardInfluencePackage(
            PackageId: "consultant_endorsement",
            Title: "Consultant Endorsement",
            Description: "Hire McKinsey to write a glowing assessment of your strategy. Expensive but credible.",
            CostPC: 5,
            ScoreBonus: 12,
            BacklashRiskPercent: 10,
            BacklashConsequenceId: null) // No backlash, just expensive
    };

    /// <summary>
    /// Gets a package by ID.
    /// </summary>
    public static BoardInfluencePackage? GetPackage(string packageId) =>
        AllPackages.FirstOrDefault(p => p.PackageId == packageId);

    /// <summary>
    /// Gets all packages the player can currently afford.
    /// </summary>
    public static IReadOnlyList<BoardInfluencePackage> GetAffordablePackages(int politicalCapital) =>
        AllPackages.Where(p => p.CostPC <= politicalCapital).ToList();

    /// <summary>
    /// Applies a board influence package.
    /// </summary>
    public static BoardInfluenceResult ApplyInfluence(
        BoardInfluencePackage package,
        int seed,
        int quarterNumber)
    {
        // Deterministic backlash check
        var rollValue = Math.Abs(HashCode.Combine(seed, quarterNumber, package.PackageId, "backlash"));
        var backlashTriggered = package.TriggersBacklash(rollValue);

        return new BoardInfluenceResult(
            PackageId: package.PackageId,
            PCSpent: package.CostPC,
            ScoreBonusApplied: package.ScoreBonus,
            BacklashTriggered: backlashTriggered,
            ScheduledConsequenceId: backlashTriggered ? package.BacklashConsequenceId : null);
    }
}
