namespace InertiCorp.Core.Cards;

/// <summary>
/// A card that can be held in the player's hand and played during the quarter.
/// Shows clear stats: effects forecast, risk, corporate tag.
/// Cards with meter affinity benefit when that meter is high, and suffer when it's low.
/// </summary>
public sealed record PlayableCard(
    string CardId,
    string Title,
    string Description,
    string FlavorText,
    OutcomeProfile Outcomes,
    int CorporateIntensity = 0,
    CardCategory Category = CardCategory.Action,
    string? ExtendedDescription = null,
    Meter? MeterAffinity = null,
    int RiskLevel = 2)
{
    /// <summary>
    /// Whether this is a "corporate/evil" card that boosts EvilScore.
    /// </summary>
    public bool IsCorporate => CorporateIntensity > 0;

    /// <summary>
    /// Display label for risk level: SAFE (1), MODERATE (2), or VOLATILE (3).
    /// </summary>
    public string RiskLabel => RiskLevel switch
    {
        1 => "SAFE",
        3 => "VOLATILE",
        _ => "MODERATE"
    };

    /// <summary>
    /// Whether this card has an affinity with a specific meter.
    /// When the affinity meter is high (60+), good outcomes more likely.
    /// When low (below 40), bad outcomes more likely.
    /// </summary>
    public bool HasMeterAffinity => MeterAffinity is not null;

    /// <summary>
    /// Gets the affinity modifier based on the current meter value.
    /// Positive modifier = better outcomes, negative = worse outcomes.
    /// Returns 0 if no affinity.
    /// </summary>
    public int GetAffinityModifier(OrgState org)
    {
        if (MeterAffinity is null) return 0;

        var meterValue = MeterAffinity.Value switch
        {
            Meter.Delivery => org.Delivery,
            Meter.Morale => org.Morale,
            Meter.Governance => org.Governance,
            Meter.Alignment => org.Alignment,
            Meter.Runway => org.Runway,
            _ => 50
        };

        // High meter (60+) = bonus to good outcomes
        // Low meter (below 40) = penalty (more bad outcomes)
        // Middle (40-59) = neutral
        if (meterValue >= 70) return 15;      // Strong bonus
        if (meterValue >= 60) return 8;       // Moderate bonus
        if (meterValue < 25) return -15;      // Strong penalty
        if (meterValue < 40) return -8;       // Moderate penalty
        return 0;
    }

    /// <summary>
    /// Gets the affinity meter name for display (full name).
    /// </summary>
    public string GetAffinityDisplay()
    {
        if (MeterAffinity is null) return "";
        return MeterAffinity.Value.ToString();
    }

    /// <summary>
    /// Gets the expected outcome effects (for forecast display).
    /// </summary>
    public IReadOnlyList<IEffect> ExpectedEffects => Outcomes.Expected;

    /// <summary>
    /// Gets a short description of expected effects for display.
    /// For Revenue cards, shows projected revenue. For others, shows meter effects.
    /// </summary>
    public string GetForecastSummary()
    {
        // For Revenue cards, show the revenue projection prominently
        if (Category == CardCategory.Revenue)
        {
            var revenueRange = GetRevenueProjection();
            if (!string.IsNullOrEmpty(revenueRange))
            {
                return revenueRange;
            }
        }

        // For non-Revenue cards, show meter effects
        var parts = new List<string>();
        foreach (var effect in Outcomes.Expected)
        {
            if (effect is MeterEffect me)
            {
                var sign = me.Delta >= 0 ? "+" : "";
                parts.Add($"{me.Meter} {sign}{me.Delta}");
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "No direct effect";
    }

    /// <summary>
    /// Gets the projected revenue range for Revenue cards.
    /// Returns format like "$15M - $35M" based on bad/good outcomes.
    /// </summary>
    public string GetRevenueProjection()
    {
        if (Category != CardCategory.Revenue) return "";

        var badProfit = GetProfitFromEffects(Outcomes.Bad);
        var expectedProfit = GetProfitFromEffects(Outcomes.Expected);
        var goodProfit = GetProfitFromEffects(Outcomes.Good);

        if (badProfit == 0 && expectedProfit == 0 && goodProfit == 0)
            return "";

        // Show the range from worst to best
        var minProfit = Math.Min(badProfit, Math.Min(expectedProfit, goodProfit));
        var maxProfit = Math.Max(badProfit, Math.Max(expectedProfit, goodProfit));

        if (minProfit == maxProfit)
            return $"💰 ${minProfit}M";

        if (minProfit < 0 && maxProfit > 0)
            return $"💰 ${minProfit}M to +${maxProfit}M";

        var minSign = minProfit >= 0 ? "+" : "";
        var maxSign = maxProfit >= 0 ? "+" : "";
        return $"💰 {minSign}${minProfit}M to {maxSign}${maxProfit}M";
    }

    /// <summary>
    /// Gets the expected revenue for Revenue cards.
    /// </summary>
    public int GetExpectedRevenue()
    {
        if (Category != CardCategory.Revenue) return 0;
        return GetProfitFromEffects(Outcomes.Expected);
    }

    /// <summary>
    /// Gets the scaled projected revenue range for Revenue cards.
    /// Applies target scaling and delivery bonus.
    /// </summary>
    public string GetScaledRevenueProjection(int targetAmount, int delivery)
    {
        if (Category != CardCategory.Revenue) return "";

        var badProfit = GetProfitFromEffects(Outcomes.Bad);
        var expectedProfit = GetProfitFromEffects(Outcomes.Expected);
        var goodProfit = GetProfitFromEffects(Outcomes.Good);

        if (badProfit == 0 && expectedProfit == 0 && goodProfit == 0)
            return "";

        // Apply scaling
        var scaledBad = ProfitCalculator.ScaleRevenueProfit(badProfit, targetAmount, delivery);
        var scaledExpected = ProfitCalculator.ScaleRevenueProfit(expectedProfit, targetAmount, delivery);
        var scaledGood = ProfitCalculator.ScaleRevenueProfit(goodProfit, targetAmount, delivery);

        // Show the range from worst to best
        var minProfit = Math.Min(scaledBad, Math.Min(scaledExpected, scaledGood));
        var maxProfit = Math.Max(scaledBad, Math.Max(scaledExpected, scaledGood));

        if (minProfit == maxProfit)
            return $"💰 ${minProfit}M";

        if (minProfit < 0 && maxProfit > 0)
            return $"💰 ${minProfit}M to +${maxProfit}M";

        var minSign = minProfit >= 0 ? "+" : "";
        var maxSign = maxProfit >= 0 ? "+" : "";
        return $"💰 {minSign}${minProfit}M to {maxSign}${maxProfit}M";
    }

    private static int GetProfitFromEffects(IReadOnlyList<IEffect> effects)
    {
        var total = 0;
        foreach (var effect in effects)
        {
            if (effect is ProfitEffect pe)
            {
                total += pe.Delta;
            }
        }
        return total;
    }

    /// <summary>
    /// Formats effects list for display.
    /// </summary>
    private static string FormatEffects(IReadOnlyList<IEffect> effects)
    {
        if (effects.Count == 0) return "No effect";

        var parts = new List<string>();
        foreach (var effect in effects)
        {
            if (effect is MeterEffect me)
            {
                var sign = me.Delta >= 0 ? "+" : "";
                parts.Add($"{me.Meter} {sign}{me.Delta}");
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "No effect";
    }

    /// <summary>
    /// Gets the best case scenario description.
    /// </summary>
    public string GetBestCaseDescription() => FormatEffects(Outcomes.Good);

    /// <summary>
    /// Gets the expected case scenario description.
    /// </summary>
    public string GetExpectedCaseDescription() => FormatEffects(Outcomes.Expected);

    /// <summary>
    /// Gets the worst case scenario description.
    /// </summary>
    public string GetWorstCaseDescription() => FormatEffects(Outcomes.Bad);

    /// <summary>
    /// Gets the full tooltip/popup content for this card.
    /// </summary>
    public string GetDetailedTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(ExtendedDescription ?? Description);
        sb.AppendLine();
        sb.AppendLine($"Best Case: {GetBestCaseDescription()}");
        sb.AppendLine($"Expected: {GetExpectedCaseDescription()}");
        sb.AppendLine($"Worst Case: {GetWorstCaseDescription()}");
        if (IsCorporate)
        {
            sb.AppendLine();
            sb.AppendLine($"Corporate Intensity: {CorporateIntensity} (increases Evil Score)");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets a list of meters that this card could reduce that are already at or near zero.
    /// Returns meter names and the potential reduction amounts for warning display.
    /// </summary>
    public IReadOnlyList<(Meter Meter, int WorstDelta)> GetZeroMeterWarnings(OrgState org)
    {
        var warnings = new List<(Meter, int)>();

        // Check all outcomes for negative meter effects
        var allEffects = new List<IEffect>();
        allEffects.AddRange(Outcomes.Bad);
        allEffects.AddRange(Outcomes.Expected);
        allEffects.AddRange(Outcomes.Good);

        // Find unique meters with negative effects
        var negativeMeters = new Dictionary<Meter, int>();
        foreach (var effect in allEffects)
        {
            if (effect is MeterEffect me && me.Delta < 0)
            {
                // Track the worst (most negative) delta for each meter
                if (!negativeMeters.TryGetValue(me.Meter, out var current) || me.Delta < current)
                {
                    negativeMeters[me.Meter] = me.Delta;
                }
            }
        }

        // Check if any of these meters are already at or near zero
        foreach (var (meter, delta) in negativeMeters)
        {
            var currentValue = meter switch
            {
                Meter.Delivery => org.Delivery,
                Meter.Morale => org.Morale,
                Meter.Governance => org.Governance,
                Meter.Alignment => org.Alignment,
                Meter.Runway => org.Runway,
                _ => 50
            };

            // Warn if meter is at 0, or if the reduction would bring it to/below 0
            if (currentValue == 0 || currentValue + delta <= 0)
            {
                warnings.Add((meter, delta));
            }
        }

        return warnings;
    }

    /// <summary>
    /// Gets a formatted warning string for zero-meter impacts.
    /// Returns empty string if no warnings.
    /// </summary>
    public string GetZeroMeterWarningText(OrgState org)
    {
        var warnings = GetZeroMeterWarnings(org);
        if (warnings.Count == 0) return "";

        var parts = warnings.Select(w => $"{w.Meter}: {w.WorstDelta}");
        return $"⚠ {string.Join(", ", parts)}";
    }
}

/// <summary>
/// Categories of playable cards.
/// </summary>
public enum CardCategory
{
    /// <summary>Proactive actions (hire, fire, reorganize)</summary>
    Action,
    /// <summary>Reactive responses (deflect, escalate, apologize)</summary>
    Response,
    /// <summary>Corporate BS (synergize, circle back, align)</summary>
    Corporate,
    /// <summary>Email-related actions (reply all, forward, ignore)</summary>
    Email,
    /// <summary>Revenue/profit-focused initiatives (sales, cost-cutting, growth)</summary>
    Revenue
}
