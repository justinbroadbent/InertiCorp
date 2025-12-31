namespace InertiCorp.Core;

/// <summary>
/// Calculates outcome weights based on state and rolls for outcomes.
/// </summary>
public static class OutcomeRoller
{
    private const int BaseGood = 20;
    private const int BaseExpected = 60;
    private const int BaseBad = 20;

    // Honeymoon period - first few quarters are more forgiving
    private const int HoneymoonEndQuarter = 3;
    private const int HoneymoonGoodBonus = 15;
    private const int HoneymoonBadReduction = 10;

    /// <summary>
    /// Calculates outcome weights based on alignment and pressure.
    /// Higher alignment shifts toward Good.
    /// Higher pressure shifts toward Bad.
    /// Early quarters (honeymoon period) get a significant bonus to good outcomes.
    /// Additional risk modifier directly increases bad outcome chance (e.g., from playing 2nd/3rd cards).
    /// Momentum bonus rewards consecutive successes.
    /// Affinity synergy bonus rewards playing multiple cards with same meter affinity.
    /// Evil path bonus rewards high evil score when playing corporate cards.
    /// </summary>
    public static (int Good, int Expected, int Bad) GetWeights(
        int alignment,
        int pressureLevel,
        int evilScore = 0,
        int additionalRiskModifier = 0,
        int quarterNumber = 1,
        int momentumBonus = 0,
        int affinitySynergyBonus = 0,
        bool isCorporateCard = false)
    {
        // Alignment modifier: +/- up to 15 based on deviation from 50
        // alignment 100 -> +10 Good, -10 Bad
        // alignment 0 -> -10 Good, +10 Bad
        int alignmentModifier = (alignment - 50) / 5;

        // Pressure modifier: each level adds to bad, subtracts from good
        // pressure 1 -> 0 modifier
        // pressure 5 -> +4 bad, -4 good
        int pressureModifier = pressureLevel - 1;

        // Evil score modifier: each point slightly increases bad outcomes
        // Represents long-term instability from corporate shortcuts
        int evilModifier = evilScore / 2;

        // Additional risk from card position (2nd card: +10%, 3rd card: +20%)
        // This directly increases bad outcome chance, taken from expected
        int cardRiskPenalty = additionalRiskModifier;

        // Honeymoon period: Q1-Q3 have better outcomes to help build reserves
        // The bonus fades each quarter: Q1: full, Q2: 2/3, Q3: 1/3
        int honeymoonGoodBonus = 0;
        int honeymoonBadReduction = 0;
        if (quarterNumber <= HoneymoonEndQuarter)
        {
            int fadeMultiplier = HoneymoonEndQuarter - quarterNumber + 1; // 3, 2, 1
            honeymoonGoodBonus = (HoneymoonGoodBonus * fadeMultiplier) / HoneymoonEndQuarter;
            honeymoonBadReduction = (HoneymoonBadReduction * fadeMultiplier) / HoneymoonEndQuarter;
        }

        // Evil path bonus: corporate cards get bonus when evil score is high
        // Evil 10+: +5% good on corporate cards
        // Evil 20+: +10% good on corporate cards
        int evilPathBonus = 0;
        if (isCorporateCard)
        {
            if (evilScore >= 20) evilPathBonus = 10;
            else if (evilScore >= 10) evilPathBonus = 5;
        }

        int good = BaseGood + alignmentModifier - pressureModifier - evilModifier + honeymoonGoodBonus
                   + momentumBonus + affinitySynergyBonus + evilPathBonus;
        int bad = BaseBad - alignmentModifier + pressureModifier + evilModifier + cardRiskPenalty - honeymoonBadReduction;
        int expected = 100 - good - bad;

        // Clamp to valid ranges
        good = Math.Clamp(good, 5, 60);  // Higher ceiling during honeymoon
        bad = Math.Clamp(bad, 5, 60);    // Allow higher bad ceiling for risky plays
        expected = 100 - good - bad;
        expected = Math.Clamp(expected, 10, 90);  // Lower floor when risk is high

        // Normalize to exactly 100
        int total = good + expected + bad;
        if (total != 100)
        {
            expected = 100 - good - bad;
        }

        return (good, expected, bad);
    }

    /// <summary>
    /// Rolls for an outcome tier using state-adjusted weights.
    /// </summary>
    public static OutcomeTier Roll(
        OutcomeProfile profile,
        int alignment,
        int pressureLevel,
        IRng rng,
        int evilScore = 0,
        int additionalRiskModifier = 0,
        int quarterNumber = 1,
        int momentumBonus = 0,
        int affinitySynergyBonus = 0,
        bool isCorporateCard = false)
    {
        var (good, expected, bad) = GetWeights(
            alignment, pressureLevel, evilScore, additionalRiskModifier,
            quarterNumber, momentumBonus, affinitySynergyBonus, isCorporateCard);
        return profile.Roll(rng, good, expected, bad);
    }

    /// <summary>
    /// Rolls for a crisis choice outcome using fixed probability profiles.
    /// - PC-cost choice (spent PC): 70% good, 20% expected, 10% bad - reliable investment
    /// - Standard choice: 20% good, 70% expected, 10% bad - safe, mostly middle ground
    /// - Evil choice: 70% good, 10% expected, 20% bad - high reward, scandal risk
    /// </summary>
    public static OutcomeTier RollCrisisChoice(OutcomeProfile profile, Choice choice, IRng rng)
    {
        int good, expected, bad;

        if (choice.HasPCCost)
        {
            // PC investment = reliable, high chance of good outcome
            // You're spending political capital to handle this well
            good = 70;
            expected = 20;
            bad = 10;
        }
        else if (choice.CorporateIntensityDelta > 0)
        {
            // Evil choice = high reward, scandal risk
            // Very likely to work brilliantly, but 1-in-5 chance the plot is exposed
            // Tempting gamble: same good chance as PC option but with scandal risk
            good = 70;
            expected = 10;
            bad = 20;
        }
        else
        {
            // Standard choice = safe, predictable
            // Most likely to get expected outcome, low variance
            good = 20;
            expected = 70;
            bad = 10;
        }

        return profile.Roll(rng, good, expected, bad);
    }
}
