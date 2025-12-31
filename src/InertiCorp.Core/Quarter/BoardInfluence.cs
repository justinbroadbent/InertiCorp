namespace InertiCorp.Core.Quarter;

/// <summary>
/// A Political Capital spending package for influencing the board.
/// Provides score bonus but may trigger governance backlash later.
/// </summary>
public sealed record BoardInfluencePackage(
    string PackageId,
    string Title,
    string Description,
    int CostPC,
    int ScoreBonus,
    int BacklashRiskPercent,
    string? BacklashConsequenceId = null)
{
    /// <summary>
    /// Whether a backlash occurs based on a deterministic roll.
    /// </summary>
    public bool TriggersBacklash(int rollValue) =>
        (rollValue % 100) < BacklashRiskPercent;
}

/// <summary>
/// Result of applying board influence.
/// </summary>
public sealed record BoardInfluenceResult(
    string PackageId,
    int PCSpent,
    int ScoreBonusApplied,
    bool BacklashTriggered,
    string? ScheduledConsequenceId);

/// <summary>
/// Board rating grades with thresholds.
/// </summary>
public enum BoardRating
{
    /// <summary>Exceptional quarter - profit and metrics exceeded expectations.</summary>
    A,
    /// <summary>Good quarter - solid performance.</summary>
    B,
    /// <summary>Average quarter - met basic expectations.</summary>
    C,
    /// <summary>Poor quarter - concerning performance.</summary>
    D,
    /// <summary>Failing quarter - serious concerns, high termination risk.</summary>
    F
}

/// <summary>
/// Board's employment decision.
/// </summary>
public enum EmploymentDecision
{
    /// <summary>CEO retains position.</summary>
    Retain,
    /// <summary>CEO is terminated with golden parachute.</summary>
    Terminate
}

/// <summary>
/// Result of the board meeting evaluation.
/// </summary>
public sealed record BoardReviewResult(
    int QuarterNumber,
    int RawScore,
    int ModifiedScore,
    BoardRating Rating,
    EmploymentDecision Decision,
    GoldenParachute? Parachute,
    BoardInfluenceResult? InfluenceApplied,
    IReadOnlyList<string> JustificationFacts)
{
    /// <summary>
    /// Human-readable rating label.
    /// </summary>
    public string RatingLabel => Rating switch
    {
        BoardRating.A => "Exceptional",
        BoardRating.B => "Good",
        BoardRating.C => "Satisfactory",
        BoardRating.D => "Concerning",
        BoardRating.F => "Unacceptable",
        _ => "Unknown"
    };
}

/// <summary>
/// Golden parachute payout details.
/// Now requires active engagement and penalizes unethical behavior.
/// </summary>
public sealed record GoldenParachute(
    int BasePayout,
    int TenureBonus,
    int PCConversion,
    int EthicsPenalty,
    int TotalPayout)
{
    /// <summary>
    /// PC to parachute conversion rate ($5M per PC point).
    /// </summary>
    public const int PCConversionRate = 5;

    /// <summary>
    /// Evil score penalty rate ($2M per evil point).
    /// </summary>
    public const int EvilPenaltyRate = 2;

    /// <summary>
    /// Tenure bonus rate ($3M per quarter survived).
    /// </summary>
    public const int TenureBonusRate = 3;

    /// <summary>
    /// Calculates golden parachute based on CEO tenure, engagement, and ethics.
    /// Inactive CEOs (no cards played) get minimal severance.
    /// </summary>
    public static GoldenParachute Calculate(
        int quartersSurvived,
        int politicalCapital,
        int evilScore,
        int totalCardsPlayed)
    {
        // Base contractual minimum
        int basePayout = 10; // $10M base

        // Engagement gate: if CEO never played cards, they get minimal parachute
        if (totalCardsPlayed == 0)
        {
            return new GoldenParachute(
                BasePayout: basePayout,
                TenureBonus: 0,
                PCConversion: 0,
                EthicsPenalty: 0,
                TotalPayout: basePayout);
        }

        // Active CEOs get full calculation
        int tenureBonus = quartersSurvived * TenureBonusRate; // $3M per quarter
        int pcConversion = politicalCapital * PCConversionRate; // $5M per PC
        int ethicsPenalty = evilScore * EvilPenaltyRate; // -$2M per evil

        int total = Math.Max(basePayout, basePayout + tenureBonus + pcConversion - ethicsPenalty);

        return new GoldenParachute(
            BasePayout: basePayout,
            TenureBonus: tenureBonus,
            PCConversion: pcConversion,
            EthicsPenalty: ethicsPenalty,
            TotalPayout: total);
    }
}
