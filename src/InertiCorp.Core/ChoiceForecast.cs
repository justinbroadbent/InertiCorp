namespace InertiCorp.Core;

/// <summary>
/// Forecast for a choice showing likely outcome and risk level.
/// Derived from state, requires no RNG.
/// </summary>
public sealed record ChoiceForecast(
    string LikelyOutcome,
    RiskLevel RiskLevel,
    IReadOnlyDictionary<Meter, (int Min, int Max)> MeterRanges)
{
    /// <summary>
    /// Creates a forecast for the given outcome profile and state.
    /// </summary>
    public static ChoiceForecast Create(OutcomeProfile profile, int alignment, int pressureLevel)
    {
        // Likely outcome is the Expected tier summary
        var likelyOutcome = SummarizeEffects(profile.Expected);

        // Risk level based on bad outcome weight
        var (_, _, badWeight) = OutcomeRoller.GetWeights(alignment, pressureLevel);
        var riskLevel = badWeight switch
        {
            <= 15 => RiskLevel.Low,
            <= 30 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        // Calculate meter ranges across all tiers
        var meterRanges = CalculateMeterRanges(profile);

        return new ChoiceForecast(likelyOutcome, riskLevel, meterRanges);
    }

    private static string SummarizeEffects(IReadOnlyList<IEffect> effects)
    {
        if (effects.Count == 0)
        {
            return "No effect";
        }

        var parts = new List<string>();
        foreach (var effect in effects)
        {
            if (effect is MeterEffect me)
            {
                var sign = me.Delta >= 0 ? "+" : "";
                parts.Add($"{me.Meter} {sign}{me.Delta}");
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "Unknown effect";
    }

    private static Dictionary<Meter, (int Min, int Max)> CalculateMeterRanges(OutcomeProfile profile)
    {
        var ranges = new Dictionary<Meter, (int Min, int Max)>();

        void ProcessEffects(IReadOnlyList<IEffect> effects)
        {
            foreach (var effect in effects)
            {
                if (effect is MeterEffect me)
                {
                    if (ranges.TryGetValue(me.Meter, out var existing))
                    {
                        ranges[me.Meter] = (
                            Math.Min(existing.Min, me.Delta),
                            Math.Max(existing.Max, me.Delta)
                        );
                    }
                    else
                    {
                        ranges[me.Meter] = (me.Delta, me.Delta);
                    }
                }
            }
        }

        ProcessEffects(profile.Good);
        ProcessEffects(profile.Expected);
        ProcessEffects(profile.Bad);

        return ranges;
    }
}
