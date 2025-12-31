namespace InertiCorp.Core.Quarter;

/// <summary>
/// Calculates board review scores, ratings, and employment decisions.
/// </summary>
public static class BoardReviewCalculator
{
    /// <summary>
    /// Score thresholds for each rating.
    /// </summary>
    public const int ThresholdA = 80;
    public const int ThresholdB = 60;
    public const int ThresholdC = 40;
    public const int ThresholdD = 20;

    /// <summary>
    /// Termination threshold - F rating triggers termination check.
    /// </summary>
    public const int TerminationThreshold = 20;

    /// <summary>
    /// Consecutive poor quarters before forced termination.
    /// </summary>
    public const int MaxConsecutivePoorQuarters = 3;

    /// <summary>
    /// Calculates the board review for a quarter.
    /// </summary>
    public static BoardReviewResult Calculate(
        int quarterNumber,
        int quarterProfit,
        OrgState org,
        CEOState ceo,
        ResourceState resources,
        BoardInfluenceResult? influence,
        int consecutivePoorQuarters,
        IRng rng)
    {
        var facts = new List<string>();

        // 1. Calculate raw score (0-100)
        int rawScore = CalculateRawScore(quarterProfit, org, ceo, facts);

        // 2. Apply influence modifier if any
        int modifiedScore = rawScore;
        if (influence is not null)
        {
            modifiedScore += influence.ScoreBonusApplied;
            facts.Add($"Board influence applied: +{influence.ScoreBonusApplied} points");

            if (influence.BacklashTriggered)
            {
                facts.Add("Governance scrutiny scheduled for next quarter");
            }
        }

        modifiedScore = Math.Clamp(modifiedScore, 0, 100);

        // 3. Determine rating
        var rating = ScoreToRating(modifiedScore);
        facts.Add($"Board Rating: {rating} (score: {modifiedScore})");

        // 4. Determine employment decision
        var decision = DetermineEmploymentDecision(
            rating, modifiedScore, consecutivePoorQuarters, rng, facts);

        // 5. Calculate parachute if terminated
        GoldenParachute? parachute = null;
        if (decision == EmploymentDecision.Terminate)
        {
            parachute = GoldenParachute.Calculate(
                ceo.QuartersSurvived,
                resources.PoliticalCapital,
                ceo.EvilScore,
                ceo.TotalCardsPlayed);
            facts.Add($"Golden Parachute: ${parachute.TotalPayout}M");
        }

        return new BoardReviewResult(
            QuarterNumber: quarterNumber,
            RawScore: rawScore,
            ModifiedScore: modifiedScore,
            Rating: rating,
            Decision: decision,
            Parachute: parachute,
            InfluenceApplied: influence,
            JustificationFacts: facts);
    }

    /// <summary>
    /// Calculates raw board score from profit and metrics.
    /// </summary>
    private static int CalculateRawScore(
        int quarterProfit,
        OrgState org,
        CEOState ceo,
        List<string> facts)
    {
        int score = 50; // Base score

        // Profit contribution (primary factor, ±30 points)
        int profitScore = quarterProfit switch
        {
            >= 20 => 30,
            >= 10 => 20,
            >= 5 => 10,
            >= 0 => 0,
            >= -5 => -10,
            >= -10 => -20,
            _ => -30
        };
        score += profitScore;
        facts.Add($"Quarterly profit ${quarterProfit}M: {profitScore:+#;-#;0} points");

        // Meter contributions (secondary, ±5 each)
        void AddMeterScore(string name, int value, int weight = 5)
        {
            int meterScore = value switch
            {
                >= 70 => weight,
                >= 50 => weight / 2,
                >= 30 => 0,
                >= 15 => -weight / 2,
                _ => -weight
            };
            if (meterScore != 0)
            {
                score += meterScore;
                facts.Add($"{name} at {value}: {meterScore:+#;-#;0} points");
            }
        }

        AddMeterScore("Delivery", org.Delivery);
        AddMeterScore("Morale", org.Morale);
        AddMeterScore("Governance", org.Governance);
        AddMeterScore("Alignment", org.Alignment, 10); // Alignment matters more
        AddMeterScore("Runway", org.Runway, 8);

        // Favorability bonus
        if (ceo.BoardFavorability >= 70)
        {
            score += 5;
            facts.Add($"High board favorability: +5 points");
        }
        else if (ceo.BoardFavorability < 30)
        {
            score -= 5;
            facts.Add($"Low board favorability: -5 points");
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Converts numeric score to letter rating.
    /// </summary>
    public static BoardRating ScoreToRating(int score)
    {
        if (score >= ThresholdA) return BoardRating.A;
        if (score >= ThresholdB) return BoardRating.B;
        if (score >= ThresholdC) return BoardRating.C;
        if (score >= ThresholdD) return BoardRating.D;
        return BoardRating.F;
    }

    /// <summary>
    /// Determines whether the CEO is retained or terminated.
    /// </summary>
    private static EmploymentDecision DetermineEmploymentDecision(
        BoardRating rating,
        int score,
        int consecutivePoorQuarters,
        IRng rng,
        List<string> facts)
    {
        // Forced termination after too many poor quarters
        if (consecutivePoorQuarters >= MaxConsecutivePoorQuarters)
        {
            facts.Add($"Terminated: {consecutivePoorQuarters} consecutive poor quarters");
            return EmploymentDecision.Terminate;
        }

        // F rating triggers termination roll
        if (rating == BoardRating.F)
        {
            // Higher chance of termination the lower the score
            int terminationChance = 30 + (TerminationThreshold - score) * 3;
            terminationChance = Math.Clamp(terminationChance, 30, 90);

            int roll = rng.NextInt(0, 100);
            if (roll < terminationChance)
            {
                facts.Add($"Board vote: Terminated ({terminationChance}% chance, rolled {roll})");
                return EmploymentDecision.Terminate;
            }
            else
            {
                facts.Add($"Board vote: Retained on probation ({terminationChance}% chance, rolled {roll})");
                return EmploymentDecision.Retain;
            }
        }

        // D rating with existing poor quarters increases risk
        if (rating == BoardRating.D && consecutivePoorQuarters >= 1)
        {
            int terminationChance = 15 * consecutivePoorQuarters;
            int roll = rng.NextInt(0, 100);
            if (roll < terminationChance)
            {
                facts.Add($"Board vote: Terminated after repeated poor performance");
                return EmploymentDecision.Terminate;
            }
        }

        return EmploymentDecision.Retain;
    }

    /// <summary>
    /// Counts consecutive poor quarters (D or F rating).
    /// </summary>
    public static bool IsPoorRating(BoardRating rating) =>
        rating == BoardRating.D || rating == BoardRating.F;
}
