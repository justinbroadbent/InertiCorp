namespace InertiCorp.Core;

/// <summary>
/// Calculates board favorability changes at Resolution.
/// Now uses nuanced profit evaluation that distinguishes between:
/// - Negative profit (actual loss) - severe
/// - Profit decline (less than before but still positive) - mild
/// - Profit growth - rewarded
/// Also penalizes sustained weak project performance.
/// </summary>
public static class FavorabilityCalculator
{
    private const int BaseSuccessReward = 8;
    private const int DirectiveFailedPenalty = -4; // Reduced - directive is now more forgiving anyway

    // Profit-based penalties (separated by severity)
    private const int NegativeProfitPenalty = -10;  // Actual loss - severe
    private const int ProfitDeclinePenalty = -3;    // Made money but less than before - mild
    private const int FlatProfitPenalty = -1;       // Roughly the same - minimal

    // Maximum favorability loss per quarter (prevents death spirals but scales with pressure)
    private const int BaseMaxFavorabilityLoss = -12;

    // Year 1 grace period (Q1-Q4)
    private const int GracePeriodQuarters = 4;

    // Evil thresholds
    private const int EvilNoticeableThreshold = 5;
    private const int EvilConcerningThreshold = 10;
    private const int EvilDangerousThreshold = 20;

    // Weak project streak penalties
    private const int WeakProjectQ1Penalty = -1;    // Warning shot
    private const int WeakProjectQ2Penalty = -3;    // "We expected a turnaround"
    private const int WeakProjectQ3Penalty = -5;    // "Losing confidence"
    private const int WeakProjectQ4PlusPenalty = -7; // "Consistent failure"

    // Critical meter thresholds
    private const int CriticalMeterThreshold = 5;   // Below 5% = critical
    private const int LowMeterThreshold = 15;       // Below 15% = concerning

    /// <summary>
    /// Calculates the favorability change for the quarter.
    /// Now distinguishes between:
    /// - Negative profit (actual loss): severe penalty
    /// - Profit decline (positive but less): mild penalty
    /// - Profit growth + directive met: reward
    /// Also caps maximum loss to prevent death spirals.
    /// Penalizes sustained weak project performance.
    /// After year 1, the board becomes progressively harder to impress.
    /// </summary>
    public static int Calculate(int lastProfit, int currentProfit, bool directiveMet, int pressureLevel, int evilScore = 0, int weakProjectStreak = 0, int quartersSurvived = 0)
    {
        bool profitUp = currentProfit > lastProfit;
        bool profitPositive = currentProfit >= 0;
        bool isSuccess = profitPositive && directiveMet;

        // Calculate weak project streak penalty and gain cap
        int streakPenalty = GetWeakProjectStreakPenalty(weakProjectStreak);
        int maxPositiveGain = GetMaxPositiveGain(weakProjectStreak);

        // Board expectations rise after year 1 - success rewards diminish
        int successReward = GetScaledSuccessReward(pressureLevel, quartersSurvived);

        if (isSuccess && profitUp)
        {
            // Full success: profit is positive, growing, and directive met
            int evilPenalty = CalculateEvilPenaltyOnSuccess(evilScore);
            int gain = successReward - evilPenalty + streakPenalty;
            // Cap positive gains if there's a weak project streak
            return Math.Min(gain, maxPositiveGain);
        }

        if (isSuccess)
        {
            // Partial success: directive met but profit didn't grow
            // Still positive outcome, just smaller reward
            int evilPenalty = CalculateEvilPenaltyOnSuccess(evilScore);
            int gain = (successReward / 2) - evilPenalty + streakPenalty;
            // Cap positive gains if there's a weak project streak
            return Math.Min(gain, maxPositiveGain);
        }

        // Calculate penalties based on profit situation
        int change = 0;

        // Profit evaluation - severity depends on whether we actually lost money
        if (currentProfit < 0)
        {
            // Actual loss - this is bad
            change += NegativeProfitPenalty;

            // Scale penalty with size of loss (additional -1 per $5M lost, capped)
            int lossScale = Math.Min(4, Math.Abs(currentProfit) / 5);
            change -= lossScale;
        }
        else if (currentProfit < lastProfit)
        {
            // Made money but less than before - mild concern
            int decline = lastProfit - currentProfit;
            if (decline > 10)
            {
                // Significant decline
                change += ProfitDeclinePenalty * 2;
            }
            else if (decline > 5)
            {
                change += ProfitDeclinePenalty;
            }
            else
            {
                // Minor decline - barely a penalty
                change += FlatProfitPenalty;
            }
        }

        // Directive failure penalty (reduced since directive is now more forgiving)
        if (!directiveMet)
        {
            change += DirectiveFailedPenalty;
        }

        // Pressure scaling on failures - full pressure impact (was pressureLevel / 2)
        change -= pressureLevel;

        // Evil scrutiny when profits are poor
        int evilScrutiny = CalculateEvilScrutinyOnFailure(evilScore);
        change -= evilScrutiny;

        // Apply weak project streak penalty (compounds with other penalties)
        change += streakPenalty;

        // Cap the maximum loss (slightly tighter after year 1)
        int maxLoss = GetScaledMaxLoss(quartersSurvived);
        return Math.Max(change, maxLoss);
    }

    /// <summary>
    /// Gets the penalty for consecutive weak project quarters.
    /// Weak = project revenue (from Revenue cards) was 0 or negative.
    /// </summary>
    private static int GetWeakProjectStreakPenalty(int streak) => streak switch
    {
        0 => 0,                      // No streak
        1 => WeakProjectQ1Penalty,   // -1: "The market is tough, we understand"
        2 => WeakProjectQ2Penalty,   // -3: "We expected a turnaround by now"
        3 => WeakProjectQ3Penalty,   // -5: "The board is losing confidence"
        _ => WeakProjectQ4PlusPenalty // -7: "Your initiatives consistently fail"
    };

    /// <summary>
    /// Gets the maximum positive favorability gain based on weak project streak.
    /// Sustained weak performance caps how much favorability you can gain.
    /// </summary>
    private static int GetMaxPositiveGain(int streak) => streak switch
    {
        0 => int.MaxValue,  // No cap
        1 => 6,             // Slight cap
        2 => 2,             // "Meeting directives isn't enough"
        _ => 0              // "No positive news until you deliver results"
    };

    /// <summary>
    /// When profits are good, board mostly ignores evil - but extreme evil still costs.
    /// </summary>
    private static int CalculateEvilPenaltyOnSuccess(int evilScore)
    {
        // Very high evil causes some concern even when profitable
        if (evilScore >= EvilDangerousThreshold)
            return 3; // "We love the profits but... the PR team is concerned"
        if (evilScore >= EvilConcerningThreshold)
            return 1; // Minor eyebrow raise

        return 0; // "Profits are up, carry on!"
    }

    /// <summary>
    /// When profits are poor, the board suddenly scrutinizes your evil ways.
    /// </summary>
    private static int CalculateEvilScrutinyOnFailure(int evilScore)
    {
        // The board becomes morally concerned when the money isn't flowing
        if (evilScore >= EvilDangerousThreshold)
            return 8; // "These layoffs AND losses? The press is having a field day!"
        if (evilScore >= EvilConcerningThreshold)
            return 4; // "Your methods are being questioned"
        if (evilScore >= EvilNoticeableThreshold)
            return 2; // "Some board members expressed... concerns"

        return 0; // Low evil, no additional scrutiny
    }

    /// <summary>
    /// Gets the success reward scaled by pressure level, tenure, and difficulty.
    /// After the grace period, the board expects more and rewards less.
    /// Difficulty modifies the base reward.
    /// </summary>
    private static int GetScaledSuccessReward(int pressureLevel, int quartersSurvived, DifficultySettings? difficulty = null)
    {
        difficulty ??= DifficultySettings.CurrentSettings;
        int baseReward = BaseSuccessReward + difficulty.SuccessRewardBonus;

        // Year 1 (Q1-Q4): Full rewards, board is patient
        if (quartersSurvived < GracePeriodQuarters)
        {
            return baseReward;
        }

        // After year 1: Pressure penalty only for Hard difficulty (negative bonus)
        // Easy/Regular: No pressure penalty on success
        // Hard: -1 at high pressure
        int pressurePenalty = 0;
        if (difficulty.SuccessRewardBonus < 0 && pressureLevel >= 5)
        {
            pressurePenalty = 1;
        }

        return Math.Max(5, baseReward - pressurePenalty); // Minimum reward of 5
    }

    /// <summary>
    /// Gets the maximum favorability loss, scaled by tenure.
    /// Early game has more protection; late game can see bigger swings.
    /// </summary>
    private static int GetScaledMaxLoss(int quartersSurvived)
    {
        // Year 1: Full protection (-12 max)
        if (quartersSurvived < GracePeriodQuarters)
        {
            return BaseMaxFavorabilityLoss;
        }

        // After year 1: Less protection
        // Q5-Q8: -14 max
        // Q9-Q12: -16 max
        // Q13+: -18 max
        int tenureQuarters = quartersSurvived - GracePeriodQuarters;
        int additionalExposure = Math.Min(6, (tenureQuarters / 4) * 2);

        return BaseMaxFavorabilityLoss - additionalExposure;
    }

    /// <summary>
    /// Calculates baseline favorability decay based on tenure and difficulty.
    /// The board's expectations rise over time - maintaining the status quo isn't enough.
    /// Returns a negative value representing passive decay.
    /// </summary>
    public static int GetTenureDecay(int quartersSurvived, DifficultySettings? difficulty = null)
    {
        difficulty ??= DifficultySettings.CurrentSettings;

        // Check if decay is enabled for this difficulty
        if (!difficulty.TenureDecayEnabled)
        {
            return 0;
        }

        // No decay until the difficulty-specified quarter
        if (quartersSurvived < difficulty.TenureDecayStartQuarter)
        {
            return 0;
        }

        // After decay starts: -1 per quarter (constant, not escalating)
        return -1;
    }

    /// <summary>
    /// Calculates the favorability adjustment for critically low meters.
    /// If any meter is critically low (below 5%), caps positive gains and may add penalty.
    /// Returns (maxPositiveGain, penalty) tuple.
    /// </summary>
    public static (int MaxPositiveGain, int Penalty, string? Reason) GetLowMeterAdjustment(OrgState org)
    {
        // Count critically low and concerning meters
        int criticalCount = 0;
        int lowCount = 0;
        var criticalMeters = new List<string>();

        if (org.Delivery < CriticalMeterThreshold) { criticalCount++; criticalMeters.Add("Delivery"); }
        else if (org.Delivery < LowMeterThreshold) lowCount++;

        if (org.Morale < CriticalMeterThreshold) { criticalCount++; criticalMeters.Add("Morale"); }
        else if (org.Morale < LowMeterThreshold) lowCount++;

        if (org.Governance < CriticalMeterThreshold) { criticalCount++; criticalMeters.Add("Governance"); }
        else if (org.Governance < LowMeterThreshold) lowCount++;

        if (org.Alignment < CriticalMeterThreshold) { criticalCount++; criticalMeters.Add("Alignment"); }
        else if (org.Alignment < LowMeterThreshold) lowCount++;

        if (org.Runway < CriticalMeterThreshold) { criticalCount++; criticalMeters.Add("Runway"); }
        else if (org.Runway < LowMeterThreshold) lowCount++;

        // Multiple critical meters: severe penalty, no positive gains
        if (criticalCount >= 2)
        {
            return (0, -5, $"Organization in crisis: {string.Join(", ", criticalMeters)} critically low");
        }

        // Single critical meter: cap positive gains at 0, small penalty
        if (criticalCount == 1)
        {
            return (0, -2, $"{criticalMeters[0]} critically low - board concerned");
        }

        // Multiple low (but not critical) meters: cap positive gains
        if (lowCount >= 3)
        {
            return (2, 0, "Multiple metrics concerning");
        }

        // No critical issues
        return (int.MaxValue, 0, null);
    }

    /// <summary>
    /// Gets the minimum projects the board expects per quarter based on tenure.
    /// The board becomes more demanding over time - a single project won't cut it after Q2.
    /// </summary>
    public static int GetExpectedProjectCount(int quartersSurvived)
    {
        // Q1-Q2: 1 project is fine (brief honeymoon)
        if (quartersSurvived < 2) return 1;

        // Q3+: Board expects 2 projects - they hired you to lead, not coast
        return 2;
    }

    /// <summary>
    /// Calculates the favorability adjustment for running fewer projects than expected.
    /// The board expects active leadership that scales with tenure.
    /// Returns (maxPositiveGain, penalty, reason) tuple.
    /// </summary>
    public static (int MaxPositiveGain, int Penalty, string? Reason) GetLowActivityAdjustment(
        int projectsPlayed, int quartersSurvived)
    {
        int expected = GetExpectedProjectCount(quartersSurvived);

        // Met expectations - no penalty
        if (projectsPlayed >= expected)
        {
            return (int.MaxValue, 0, null);
        }

        int shortfall = expected - projectsPlayed;

        // Q1-Q2: Brief honeymoon - no penalty for playing 1 project
        if (quartersSurvived < 2)
        {
            return (int.MaxValue, 0, null);
        }

        // After honeymoon: Running fewer projects than expected caps gains and adds penalty
        // Penalties increase with tenure - the board grows impatient with minimal effort
        int tenureMultiplier = 1 + (quartersSurvived / 3); // +1 every 3 quarters (faster scaling)

        if (projectsPlayed == 0)
        {
            // No projects at all = severe penalty that scales with tenure
            return (0, -5 * tenureMultiplier, "Board expects active strategic leadership");
        }
        else if (shortfall >= 1)
        {
            // Below expectations (1 when expecting 2+) - no positive gains, penalty scales with tenure
            // This specifically targets the "minimal effort" exploit
            return (0, -4 * tenureMultiplier, $"Board expected {expected}+ projects, only {projectsPlayed} delivered");
        }

        return (int.MaxValue, 0, null);
    }
}
