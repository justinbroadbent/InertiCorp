using System.Collections.Immutable;

namespace InertiCorp.Core;

/// <summary>
/// Immutable record tracking the CEO's tenure, board pressure, favorability, and financials.
/// Now includes profit trajectory tracking for smoothed evaluation.
/// </summary>
public sealed record CEOState(
    int BoardPressureLevel,
    int QuartersSurvived,
    int BoardFavorability,
    bool IsOusted,
    int TotalProfit,
    int EvilScore,
    int LastQuarterProfit = 0,
    int CurrentQuarterProfit = 0,
    int ConsecutiveSuccesses = 0,
    int AccumulatedBonus = 0,
    int QuarterlyBonusAwarded = 0,
    bool HasRetired = false,
    int EvilScoreLastQuarter = 0,
    int ConsecutiveNegativeQuarters = 0,
    int ConsecutiveWeakProjectQuarters = 0,
    int TotalCardsPlayed = 0,
    ImmutableList<int>? RecentProfits = null)
{
    /// <summary>
    /// Number of recent quarters to track for profit smoothing.
    /// </summary>
    private const int ProfitHistorySize = 3;

    /// <summary>
    /// Default retirement threshold for backwards compatibility.
    /// Actual threshold is determined by difficulty settings.
    /// </summary>
    public const int DefaultRetirementThreshold = 150;

    /// <summary>
    /// Gets the retirement threshold based on current difficulty.
    /// </summary>
    public static int RetirementThreshold => DifficultySettings.CurrentSettings.RetirementThreshold;

    /// <summary>
    /// Whether the CEO has accumulated enough bonus to retire.
    /// Uses difficulty-based threshold.
    /// </summary>
    public bool CanRetire => AccumulatedBonus >= DifficultySettings.CurrentSettings.RetirementThreshold;

    /// <summary>
    /// Initial CEO state based on current difficulty settings.
    /// Favorability varies by difficulty.
    /// </summary>
    public static CEOState Initial => new(
        BoardPressureLevel: 1,
        QuartersSurvived: 0,
        BoardFavorability: DifficultySettings.CurrentSettings.StartingFavorability,
        IsOusted: false,
        TotalProfit: 0,
        EvilScore: 0,
        LastQuarterProfit: 0,
        CurrentQuarterProfit: 0
    );

    /// <summary>
    /// Golden parachute payout calculated at game end (in millions).
    /// Requires active engagement - inactive CEOs get minimal severance.
    /// Note: PC conversion is added separately in ScoreCalculator.
    /// </summary>
    public int ParachutePayout
    {
        get
        {
            const int BasePayout = 10; // $10M contractual minimum

            // Inactive CEOs get minimal parachute
            if (TotalCardsPlayed == 0)
            {
                return BasePayout;
            }

            // Active CEOs: Base + Tenure - Evil penalty
            var tenureBonus = QuartersSurvived * 3; // $3M per quarter
            var ethicsPenalty = EvilScore * 2; // -$2M per evil point

            return Math.Max(BasePayout, BasePayout + tenureBonus - ethicsPenalty);
        }
    }

    /// <summary>
    /// Gets a description of the parachute amount for display.
    /// </summary>
    public string ParachuteDescription => TotalCardsPlayed == 0
        ? "minimal severance ($10M) - no strategic initiatives"
        : $"${ParachutePayout}M";

    /// <summary>
    /// Maximum board pressure level (capped to prevent runaway difficulty).
    /// </summary>
    private const int MaxPressureLevel = 8;

    /// <summary>
    /// Returns a new CEOState after completing a quarter.
    /// Increments QuartersSurvived. Pressure increases every 2 quarters for gradual difficulty curve.
    /// </summary>
    public CEOState WithQuarterComplete()
    {
        var newQuarters = QuartersSurvived + 1;
        // Pressure increases every 2 quarters: Q2->1, Q4->2, Q6->3, etc.
        var newPressure = Math.Min(newQuarters / 2, MaxPressureLevel);
        return this with
        {
            BoardPressureLevel = newPressure,
            QuartersSurvived = newQuarters
        };
    }

    /// <summary>
    /// Returns a new CEOState with changed favorability (clamped 0-100).
    /// </summary>
    public CEOState WithFavorabilityChange(int delta) => this with
    {
        BoardFavorability = Math.Clamp(BoardFavorability + delta, 0, 100)
    };

    /// <summary>
    /// Returns a new CEOState marked as ousted.
    /// </summary>
    public CEOState WithOusted() => this with { IsOusted = true };

    /// <summary>
    /// Returns a new CEOState with added profit.
    /// </summary>
    public CEOState WithProfitAdded(int profit) => this with
    {
        TotalProfit = TotalProfit + profit
    };

    /// <summary>
    /// Returns a new CEOState with changed evil score.
    /// </summary>
    public CEOState WithEvilScoreChange(int delta) => this with
    {
        EvilScore = EvilScore + delta
    };

    /// <summary>
    /// Returns a new CEOState with updated success streak.
    /// Good outcomes increase the streak, bad outcomes reset it.
    /// </summary>
    public CEOState WithSuccessResult(bool wasSuccess) => this with
    {
        ConsecutiveSuccesses = wasSuccess ? ConsecutiveSuccesses + 1 : 0
    };

    /// <summary>
    /// Momentum bonus percentage based on consecutive successes.
    /// 2 in a row: +3% good outcome chance
    /// 3+ in a row: +5% good outcome chance
    /// </summary>
    public int MomentumBonus => ConsecutiveSuccesses switch
    {
        >= 3 => 5,
        2 => 3,
        _ => 0
    };

    /// <summary>
    /// Evil score increase this quarter (for bonus calculation).
    /// </summary>
    public int EvilDeltaThisQuarter => EvilScore - EvilScoreLastQuarter;

    /// <summary>
    /// Returns a new CEOState with bonus awarded.
    /// </summary>
    public CEOState WithBonusAwarded(int bonus) => this with
    {
        AccumulatedBonus = AccumulatedBonus + bonus,
        QuarterlyBonusAwarded = bonus
    };

    /// <summary>
    /// Returns a new CEOState marked as retired (victory condition).
    /// </summary>
    public CEOState WithRetirement() => this with { HasRetired = true };

    /// <summary>
    /// Returns a new CEOState with evil score snapshot for next quarter tracking.
    /// Call at the start of each quarter.
    /// </summary>
    public CEOState WithEvilSnapshotForQuarter() => this with
    {
        EvilScoreLastQuarter = EvilScore
    };

    /// <summary>
    /// Gets the smoothed profit over recent quarters (average of last N quarters).
    /// Used for board evaluation to reduce variance impact.
    /// </summary>
    public int SmoothedProfit
    {
        get
        {
            var profits = RecentProfits ?? ImmutableList<int>.Empty;
            if (profits.Count == 0) return CurrentQuarterProfit;
            return (int)profits.Average();
        }
    }

    /// <summary>
    /// Gets the profit trajectory: positive = improving, negative = declining.
    /// Compares recent average to older average.
    /// </summary>
    public int ProfitTrajectory
    {
        get
        {
            var profits = RecentProfits ?? ImmutableList<int>.Empty;
            if (profits.Count < 2) return 0;

            // Compare most recent to average of older
            var recent = profits[^1];
            var older = profits.Take(profits.Count - 1).Average();
            return recent - (int)older;
        }
    }

    /// <summary>
    /// Whether the profit trajectory is positive (improving over time).
    /// </summary>
    public bool IsProfitImproving => ProfitTrajectory > 0;

    /// <summary>
    /// Returns a new CEOState with profit added to the tracking history.
    /// </summary>
    public CEOState WithProfitRecorded(int quarterProfit)
    {
        var profits = RecentProfits ?? ImmutableList<int>.Empty;

        // Add new profit, keeping only last N quarters
        profits = profits.Add(quarterProfit);
        if (profits.Count > ProfitHistorySize)
        {
            profits = profits.RemoveAt(0);
        }

        // Track consecutive negative quarters
        int newConsecutiveNegative = quarterProfit < 0
            ? ConsecutiveNegativeQuarters + 1
            : 0;

        return this with
        {
            RecentProfits = profits,
            ConsecutiveNegativeQuarters = newConsecutiveNegative
        };
    }

    /// <summary>
    /// Whether the CEO is in a sustained loss situation (multiple negative quarters).
    /// </summary>
    public bool IsInSustainedLoss => ConsecutiveNegativeQuarters >= 2;

    /// <summary>
    /// Updates the weak project quarters streak based on project revenue this quarter.
    /// Weak = project revenue (from Revenue cards) is 0 or negative.
    /// </summary>
    public CEOState WithProjectPerformanceRecorded(int projectRevenue)
    {
        int newStreak = projectRevenue <= 0
            ? ConsecutiveWeakProjectQuarters + 1
            : 0;

        return this with { ConsecutiveWeakProjectQuarters = newStreak };
    }

    /// <summary>
    /// Whether the CEO has a sustained weak project performance streak (2+ quarters).
    /// </summary>
    public bool HasWeakProjectStreak => ConsecutiveWeakProjectQuarters >= 2;

    /// <summary>
    /// Records cards played this quarter, adding to lifetime total.
    /// </summary>
    public CEOState WithCardsPlayedRecorded(int cardsPlayedThisQuarter)
    {
        return this with { TotalCardsPlayed = TotalCardsPlayed + cardsPlayedThisQuarter };
    }
}
