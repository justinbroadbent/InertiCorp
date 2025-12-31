namespace InertiCorp.Core;

/// <summary>
/// Calculates final scores and quarterly bonuses for the CEO.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// PC to parachute conversion rate for final score ($5M per PC point).
    /// </summary>
    public const int PCConversionRate = 5;

    /// <summary>
    /// Score bonus per project implemented ($1M per card played).
    /// </summary>
    public const int ProjectsBonusRate = 1;

    /// <summary>
    /// Calculates the final score when the game ends.
    /// Simplified formula: (Accumulated Bonus + Golden Parachute + PC Conversion + Projects Bonus) × Multiplier
    /// Retirement gets a 2x multiplier, ouster gets 0.5x.
    /// </summary>
    public static int CalculateFinalScore(CEOState ceo, ResourceState resources)
    {
        // Parachute already includes tenure bonus and evil penalty
        var parachute = ceo.ParachutePayout;

        // PC converts to parachute at $5M per point
        var pcConversion = resources.PoliticalCapital * PCConversionRate;

        // Projects bonus: $1M per project implemented
        var projectsBonus = ceo.TotalCardsPlayed * ProjectsBonusRate;

        var baseScore = ceo.AccumulatedBonus + parachute + pcConversion + projectsBonus;

        var multiplier = ceo.HasRetired ? 2.0 : 0.5;
        return Math.Max(0, (int)(baseScore * multiplier));
    }

    /// <summary>
    /// Calculates the quarterly bonus awarded by the board.
    /// Based on survival, meeting directives, profit growth, org health, and ethics.
    /// Target: $7-10M average per quarter, $50M threshold reachable in 6-8 quarters.
    /// </summary>
    public static (int Bonus, IReadOnlyList<string> Reasons) CalculateQuarterlyBonus(
        CEOState ceo,
        OrgState org,
        bool directiveMet,
        int profitDelta)
    {
        var bonus = 0;
        var reasons = new List<string>();

        // Base survival bonus - you get paid just for showing up
        bonus += 2;
        reasons.Add("+$2M: Quarterly base compensation");

        // Award conditions
        if (directiveMet)
        {
            bonus += 4;
            reasons.Add("+$4M: Met board directive");
        }

        if (profitDelta > 0)
        {
            bonus += 3;
            reasons.Add("+$3M: Profit growth quarter-over-quarter");
        }

        var allMetersHealthy = org.Delivery >= 40 && org.Morale >= 40 &&
            org.Governance >= 40 && org.Alignment >= 40 && org.Runway >= 40;
        if (allMetersHealthy)
        {
            bonus += 3;
            reasons.Add("+$3M: All organizational metrics healthy");
        }

        if (ceo.BoardFavorability >= 70)
        {
            bonus += 2;
            reasons.Add("+$2M: Strong board confidence");
        }

        if (ceo.EvilDeltaThisQuarter <= 0)
        {
            bonus += 2;
            reasons.Add("+$2M: Maintained ethical standards");
        }

        // Penalty conditions
        if (!directiveMet)
        {
            bonus -= 3;
            reasons.Add("-$3M: Failed board directive");
        }

        // Count meters below 20
        var criticalMeters = new List<string>();
        if (org.Delivery < 20) criticalMeters.Add("Delivery");
        if (org.Morale < 20) criticalMeters.Add("Morale");
        if (org.Governance < 20) criticalMeters.Add("Governance");
        if (org.Alignment < 20) criticalMeters.Add("Alignment");
        if (org.Runway < 20) criticalMeters.Add("Runway");

        if (criticalMeters.Count > 0)
        {
            var penalty = criticalMeters.Count * 2;
            bonus -= penalty;
            reasons.Add($"-${penalty}M: Critical metrics ({string.Join(", ", criticalMeters)})");
        }

        if (ceo.EvilScore >= 15)
        {
            bonus -= 3;
            reasons.Add("-$3M: Reputation concerns (Evil 15+)");
        }

        // Minimum bonus is 0
        bonus = Math.Max(0, bonus);

        return (bonus, reasons);
    }

    /// <summary>
    /// Generates a breakdown of the final score for display.
    /// </summary>
    public static FinalScoreBreakdown GetScoreBreakdown(CEOState ceo, ResourceState resources)
    {
        var accumulatedBonus = ceo.AccumulatedBonus;
        var parachute = ceo.ParachutePayout;
        var pcConversion = resources.PoliticalCapital * PCConversionRate;
        var projectsBonus = ceo.TotalCardsPlayed * ProjectsBonusRate;

        var subtotal = accumulatedBonus + parachute + pcConversion + projectsBonus;
        var multiplier = ceo.HasRetired ? 2.0 : 0.5;
        var finalScore = Math.Max(0, (int)(subtotal * multiplier));

        return new FinalScoreBreakdown(
            AccumulatedBonus: accumulatedBonus,
            GoldenParachute: parachute,
            PCConversion: pcConversion,
            PoliticalCapital: resources.PoliticalCapital,
            ProjectsBonus: projectsBonus,
            TotalProjects: ceo.TotalCardsPlayed,
            Subtotal: subtotal,
            Multiplier: multiplier,
            MultiplierReason: ceo.HasRetired ? "Graceful Retirement" : "Ousted",
            FinalScore: finalScore
        );
    }
}

/// <summary>
/// Breakdown of final score components for display.
/// Simplified: Bonus + Parachute (includes tenure/evil) + PC Conversion + Projects Bonus.
/// </summary>
public sealed record FinalScoreBreakdown(
    int AccumulatedBonus,
    int GoldenParachute,
    int PCConversion,
    int PoliticalCapital,
    int ProjectsBonus,
    int TotalProjects,
    int Subtotal,
    double Multiplier,
    string MultiplierReason,
    int FinalScore);
