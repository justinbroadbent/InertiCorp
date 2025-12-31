namespace InertiCorp.Core;

/// <summary>
/// Calculates quarterly profit from base operations.
/// Base operations profit is independent of CEO actions - it represents
/// the company running on autopilot. Projects add/subtract from this.
/// Profits are in millions (e.g., 150 = $150M).
/// </summary>
public static class ProfitCalculator
{
    // Base operations parameters
    private const int BaseOperationsMin = 80;   // Minimum base operations profit
    private const int BaseOperationsMax = 140;  // Maximum base operations profit
    private const int NegativeChance = 8;       // 8% chance of a bad quarter

    // Organic growth rate per quarter (2% - represents market/company growth)
    private const double QuarterlyGrowthRate = 0.02;

    // Meter bonuses/penalties
    private const int HighMeterThreshold = 60;
    private const int LowMeterThreshold = 35;
    private const int MeterBonus = 10;
    private const int MeterPenalty = 15;

    // Revenue card scaling baseline (cards balanced around ~$25M target)
    private const int BaselineTarget = 25;

    /// <summary>
    /// Calculates the BASE OPERATIONS profit for the quarter (in millions).
    /// This represents the company running on autopilot - independent of CEO projects.
    /// High organizational health gives bonuses; low health gives penalties.
    /// Small chance (~8%) of a negative quarter (market downturn, etc).
    /// </summary>
    public static int CalculateBaseOperations(OrgState org, IRng rng)
    {
        // Check for bad quarter (market downturn, supply chain issues, etc)
        if (rng.NextInt(0, 100) < NegativeChance)
        {
            // Bad quarter: -30 to +20
            return rng.NextInt(-30, 21);
        }

        // Normal quarter: random base in range
        int baseProfit = rng.NextInt(BaseOperationsMin, BaseOperationsMax + 1);

        // Apply meter modifiers
        int modifiers = 0;

        // Delivery affects operational efficiency
        if (org.Delivery >= HighMeterThreshold)
            modifiers += MeterBonus;
        else if (org.Delivery < LowMeterThreshold)
            modifiers -= MeterPenalty;

        // Runway affects financial stability
        if (org.Runway >= HighMeterThreshold)
            modifiers += MeterBonus;
        else if (org.Runway < LowMeterThreshold)
            modifiers -= MeterPenalty;

        // Governance affects operational risk
        if (org.Governance >= HighMeterThreshold)
            modifiers += MeterBonus / 2;
        else if (org.Governance < LowMeterThreshold)
            modifiers -= MeterPenalty / 2;

        // Small random variance (+/- 15)
        int variance = rng.NextInt(-15, 16);

        return baseProfit + modifiers + variance;
    }

    /// <summary>
    /// Calculates total quarterly profit: Base Operations + Project Impact.
    /// </summary>
    public static int CalculateTotal(int baseOperations, int projectImpact)
    {
        return baseOperations + projectImpact;
    }

    /// <summary>
    /// Formats profit for display (e.g., 150 -> "$150M", -20 -> "-$20M").
    /// </summary>
    public static string Format(int profitInMillions)
    {
        if (profitInMillions < 0)
        {
            if (profitInMillions <= -1000)
                return $"-${Math.Abs(profitInMillions) / 1000.0:F1}B";
            return $"-${Math.Abs(profitInMillions)}M";
        }

        if (profitInMillions >= 1000)
            return $"${profitInMillions / 1000.0:F1}B";
        return $"${profitInMillions}M";
    }

    /// <summary>
    /// Formats profit with explicit sign for display (e.g., "+$15M" or "-$20M").
    /// </summary>
    public static string FormatWithSign(int profitInMillions)
    {
        if (profitInMillions >= 0)
            return $"+${profitInMillions}M";
        return $"-${Math.Abs(profitInMillions)}M";
    }

    /// <summary>
    /// Calculates base operations with organic growth over quarters.
    /// Base operations grows 2% per quarter to represent market/company growth.
    /// </summary>
    public static int CalculateBaseOperations(OrgState org, IRng rng, int quartersElapsed)
    {
        // Apply organic growth multiplier
        var growthMultiplier = 1.0 + (quartersElapsed * QuarterlyGrowthRate);
        var adjustedMin = (int)(BaseOperationsMin * growthMultiplier);
        var adjustedMax = (int)(BaseOperationsMax * growthMultiplier);

        // Check for bad quarter (market downturn, supply chain issues, etc)
        if (rng.NextInt(0, 100) < NegativeChance)
        {
            // Bad quarter: scaled negative range
            var badMin = (int)(-30 * growthMultiplier);
            var badMax = (int)(21 * growthMultiplier);
            return rng.NextInt(badMin, badMax);
        }

        // Normal quarter: random base in adjusted range
        int baseProfit = rng.NextInt(adjustedMin, adjustedMax + 1);

        // Apply meter modifiers (scaled with growth)
        int modifiers = 0;
        var scaledBonus = (int)(MeterBonus * growthMultiplier);
        var scaledPenalty = (int)(MeterPenalty * growthMultiplier);

        // Delivery affects operational efficiency
        if (org.Delivery >= HighMeterThreshold)
            modifiers += scaledBonus;
        else if (org.Delivery < LowMeterThreshold)
            modifiers -= scaledPenalty;

        // Runway affects financial stability
        if (org.Runway >= HighMeterThreshold)
            modifiers += scaledBonus;
        else if (org.Runway < LowMeterThreshold)
            modifiers -= scaledPenalty;

        // Governance affects operational risk
        if (org.Governance >= HighMeterThreshold)
            modifiers += scaledBonus / 2;
        else if (org.Governance < LowMeterThreshold)
            modifiers -= scaledPenalty / 2;

        // Small random variance (scaled)
        var scaledVariance = (int)(15 * growthMultiplier);
        int variance = rng.NextInt(-scaledVariance, scaledVariance + 1);

        return baseProfit + modifiers + variance;
    }

    /// <summary>
    /// Gets revenue multiplier based on Delivery meter.
    /// High delivery = operational excellence = better revenue execution.
    /// Modest bonus to avoid revenue strategy dominating.
    /// </summary>
    public static double GetDeliveryMultiplier(int delivery) => delivery switch
    {
        >= 90 => 1.05,  // +5%
        >= 80 => 1.03,  // +3%
        _ => 1.0        // No bonus
    };

    /// <summary>
    /// Gets target-relative scaling for revenue cards.
    /// Keeps card profits proportional to current targets for strategic play.
    /// </summary>
    public static double GetTargetScaling(int targetAmount)
    {
        return Math.Max(0.5, (double)targetAmount / BaselineTarget);
    }

    /// <summary>
    /// Gets diminishing returns multiplier for playing multiple Revenue cards in one quarter.
    /// 1st: 100%, 2nd: 65%, 3rd: 35%
    /// This prevents revenue-only strategies from dominating.
    /// </summary>
    public static double GetRevenueDiminishingReturns(int revenueCardIndex) => revenueCardIndex switch
    {
        0 => 1.0,   // First revenue card: full value
        1 => 0.65,  // Second: 65%
        _ => 0.35   // Third+: 35%
    };

    /// <summary>
    /// Scales a revenue card's profit based on current target, delivery, and diminishing returns.
    /// </summary>
    public static int ScaleRevenueProfit(int baseProfit, int targetAmount, int delivery, int revenueCardsPlayedBefore = 0)
    {
        var targetScale = GetTargetScaling(targetAmount);
        var deliveryMult = GetDeliveryMultiplier(delivery);
        var diminishingReturns = GetRevenueDiminishingReturns(revenueCardsPlayedBefore);
        return (int)(baseProfit * targetScale * deliveryMult * diminishingReturns);
    }
}
