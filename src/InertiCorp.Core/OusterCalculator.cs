namespace InertiCorp.Core;

/// <summary>
/// Calculates ouster risk and performs the board vote at Resolution using d20 mechanics.
/// Now considers profit trajectory and consecutive negative quarters.
/// </summary>
public static class OusterCalculator
{
    /// <summary>
    /// Honeymoon period - first N quarters have reduced ouster risk.
    /// The board gives new CEOs time to prove themselves.
    /// </summary>
    private const int HoneymoonQuarters = 8;

    /// <summary>
    /// Gets the ouster threshold for a d20 roll (oust if roll <= threshold).
    /// Now more forgiving for single bad quarters, harsher for sustained losses.
    /// </summary>
    /// <returns>D20 threshold (0-19). 0 means safe, 1 means oust on natural 1 only.</returns>
    public static int GetOusterThreshold(
        int favorability,
        int pressureLevel,
        int quartersSurvived = 0,
        int evilScore = 0,
        bool directiveMet = false,
        bool profitPositive = false,
        bool profitImproving = false,
        int consecutiveNegativeQuarters = 0,
        int consecutiveWeakProjectQuarters = 0,
        int cardsPlayedThisQuarter = 1)
    {
        int baseThreshold;

        // More forgiving favorability zones
        if (favorability >= 55)
        {
            // Safe zone - board is confident, no vote needed
            return 0;
        }
        else if (favorability >= 40)
        {
            // Caution zone: oust on 1 (5%)
            baseThreshold = 1;
        }
        else if (favorability >= 25)
        {
            // Danger zone: oust on 1-2 (10%)
            baseThreshold = 2;
        }
        else if (favorability >= 10)
        {
            // High danger: oust on 1-3 (15%)
            baseThreshold = 3;
        }
        else
        {
            // Critical zone: oust on 1-4 (20%)
            baseThreshold = 4;
        }

        // Pressure adds +1 to threshold per 2 levels (reduced impact)
        int threshold = baseThreshold + (pressureLevel / 2);

        // Extended honeymoon period with gradual phase-out
        // Q1-4: -4 (very safe), Q5-6: -2 (safe), Q7-8: -1 (somewhat safe), Q9+: full risk
        if (quartersSurvived < 4)
        {
            threshold = Math.Max(0, threshold - 4);
        }
        else if (quartersSurvived < 6)
        {
            threshold = Math.Max(0, threshold - 2);
        }
        else if (quartersSurvived < HoneymoonQuarters)
        {
            threshold = Math.Max(0, threshold - 1);
        }

        // Ethics bonus: low evil score reduces ouster risk
        if (evilScore == 0)
        {
            threshold = Math.Max(0, threshold - 2);
        }
        else if (evilScore < 5)
        {
            threshold = Math.Max(0, threshold - 1);
        }

        // Performance bonuses (can stack) - BUT only if CEO is actively engaged
        // Base operations profit doesn't count - board expects strategic initiative
        bool isActivelyEngaged = cardsPlayedThisQuarter > 0;

        if (directiveMet && isActivelyEngaged)
        {
            threshold = Math.Max(0, threshold - 2);
        }
        if (profitPositive && isActivelyEngaged)
        {
            threshold = Math.Max(0, threshold - 1);
        }
        if (profitImproving && isActivelyEngaged)
        {
            threshold = Math.Max(0, threshold - 1);
        }

        // Consecutive negative quarters increases risk significantly
        // This is the penalty for sustained losses, not single bad quarters
        if (consecutiveNegativeQuarters >= 3)
        {
            threshold += 4; // Very dangerous
        }
        else if (consecutiveNegativeQuarters >= 2)
        {
            threshold += 2; // Concerning
        }

        // Consecutive weak project quarters - board loses patience with inactive CEO
        // This is SEPARATE from negative profit - you can have positive base ops but no initiative
        if (consecutiveWeakProjectQuarters >= 6)
        {
            // Automatic ouster - board has had enough of a do-nothing CEO
            return 20; // 100% chance (roll 1-20 always triggers)
        }
        else if (consecutiveWeakProjectQuarters >= 4)
        {
            threshold += 6; // Extremely dangerous - final warning territory
        }
        else if (consecutiveWeakProjectQuarters >= 2)
        {
            threshold += 3; // Board is getting suspicious
        }

        // Cap at 14 (70% chance) for normal cases - but allow 20 for auto-ouster
        return Math.Min(threshold, consecutiveWeakProjectQuarters >= 6 ? 20 : 14);
    }

    /// <summary>
    /// Legacy overload for backward compatibility.
    /// </summary>
    public static int GetOusterThreshold(
        int favorability,
        int pressureLevel,
        int quartersSurvived,
        int evilScore,
        bool directiveMet,
        bool profitGrew)
    {
        return GetOusterThreshold(
            favorability, pressureLevel, quartersSurvived, evilScore,
            directiveMet, profitPositive: profitGrew, profitImproving: profitGrew,
            consecutiveNegativeQuarters: 0);
    }

    /// <summary>
    /// Calculates the ouster risk percentage based on favorability and pressure.
    /// </summary>
    /// <returns>Risk percentage (0-70).</returns>
    public static int GetOusterRisk(
        int favorability,
        int pressureLevel,
        int quartersSurvived = 0,
        int evilScore = 0,
        bool directiveMet = false,
        bool profitGrew = false)
    {
        int threshold = GetOusterThreshold(favorability, pressureLevel, quartersSurvived, evilScore, directiveMet, profitGrew);
        // Convert d20 threshold to percentage: threshold / 20 * 100 = threshold * 5
        return threshold * 5;
    }

    /// <summary>
    /// Rolls a d20 for ouster and returns whether the CEO was ousted.
    /// Uses new trajectory-aware evaluation.
    /// </summary>
    public static bool RollForOuster(
        int favorability,
        int pressureLevel,
        IRng rng,
        int quartersSurvived,
        int evilScore,
        bool directiveMet,
        bool profitPositive,
        bool profitImproving,
        int consecutiveNegativeQuarters,
        int consecutiveWeakProjectQuarters = 0,
        int cardsPlayedThisQuarter = 1)
    {
        int threshold = GetOusterThreshold(
            favorability, pressureLevel, quartersSurvived, evilScore,
            directiveMet, profitPositive, profitImproving, consecutiveNegativeQuarters,
            consecutiveWeakProjectQuarters, cardsPlayedThisQuarter);

        if (threshold == 0)
        {
            return false;
        }

        // Roll d20 (1-20)
        int roll = rng.NextInt(1, 21);
        return roll <= threshold;
    }

    /// <summary>
    /// Legacy overload for backward compatibility.
    /// </summary>
    public static bool RollForOuster(
        int favorability,
        int pressureLevel,
        IRng rng,
        int quartersSurvived = 0,
        int evilScore = 0,
        bool directiveMet = false,
        bool profitGrew = false)
    {
        return RollForOuster(
            favorability, pressureLevel, rng, quartersSurvived, evilScore,
            directiveMet, profitPositive: profitGrew, profitImproving: profitGrew,
            consecutiveNegativeQuarters: 0);
    }

    /// <summary>
    /// Gets a description of the current ouster risk for UI display.
    /// </summary>
    public static string GetRiskDescription(
        int favorability,
        int pressureLevel,
        int quartersSurvived = 0,
        int evilScore = 0,
        bool directiveMet = false,
        bool profitGrew = false)
    {
        int threshold = GetOusterThreshold(favorability, pressureLevel, quartersSurvived, evilScore, directiveMet, profitGrew);
        int risk = threshold * 5;

        var notes = new List<string>();
        if (quartersSurvived < HoneymoonQuarters) notes.Add("honeymoon");
        if (evilScore < 10) notes.Add("ethical");
        if (directiveMet || profitGrew) notes.Add("performing");

        var noteStr = notes.Count > 0 ? $" ({string.Join(", ", notes)})" : "";

        return threshold switch
        {
            0 => $"Safe - Board is confident{noteStr}",
            1 => $"Low ({risk}%) - Oust on natural 1{noteStr}",
            2 => $"Moderate ({risk}%) - Oust on 1-2{noteStr}",
            <= 4 => $"Elevated ({risk}%) - Oust on 1-{threshold}{noteStr}",
            <= 8 => $"High ({risk}%) - Oust on 1-{threshold}{noteStr}",
            _ => $"Critical ({risk}%) - Oust on 1-{threshold}{noteStr}"
        };
    }
}
