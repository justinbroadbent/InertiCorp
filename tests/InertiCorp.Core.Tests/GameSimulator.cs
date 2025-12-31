using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Crisis;

namespace InertiCorp.Core.Tests;

/// <summary>
/// Automated game simulator for balance testing.
/// Plays games using different strategies and collects metrics.
/// </summary>
public class GameSimulator
{
    public enum Strategy
    {
        Random,           // Random choices
        Conservative,     // Play 0-1 cards, hoard PC, avoid evil
        Aggressive,       // Play max cards, take risks
        Balanced,         // Play 1-2 cards, moderate risk
        RevenueHunter,    // Prioritize revenue cards
        MeterManager,     // Keep meters balanced
        DoNothing,        // Never play cards - tests anti-coasting mechanics
        MinimalEffort     // Play exactly 1 revenue card per quarter - tests the "coasting" exploit
    }

    public record GameResult
    {
        public int QuartersSurvived { get; init; }
        public int TotalProfit { get; init; }
        public int FinalFavorability { get; init; }
        public int FinalEvilScore { get; init; }
        public int FinalPC { get; init; }
        public OrgState FinalOrg { get; init; } = new OrgState(50, 50, 50, 50, 50);
        public int CardsPlayed { get; init; }
        public int CrisesHandled { get; init; }
        public string DeathReason { get; init; } = "";
        public Strategy UsedStrategy { get; init; }
        public int Seed { get; init; }
        // Retirement metrics
        public bool Retired { get; init; }
        public int AccumulatedBonus { get; init; }
        public int FinalScore { get; init; }
        public int QuarterRetirementAvailable { get; init; } // 0 if never available
    }

    public record SimulationStats
    {
        public int GamesPlayed { get; init; }
        public Strategy Strategy { get; init; }
        public double AvgQuartersSurvived { get; init; }
        public double MedianQuartersSurvived { get; init; }
        public int MaxQuartersSurvived { get; init; }
        public int MinQuartersSurvived { get; init; }
        public double AvgTotalProfit { get; init; }
        public double AvgFinalFavorability { get; init; }
        public double AvgEvilScore { get; init; }
        public double AvgCardsPerQuarter { get; init; }
        public Dictionary<string, int> DeathReasons { get; init; } = new();
        public double SurvivalRate5Q { get; init; }  // % surviving 5+ quarters
        public double SurvivalRate10Q { get; init; } // % surviving 10+ quarters
        public double SurvivalRate20Q { get; init; } // % surviving 20+ quarters
        // Retirement metrics
        public double RetirementRate { get; init; }  // % that retired successfully
        public double AvgRetirementQuarter { get; init; }  // When they retired (if they did)
        public double AvgQuarterRetirementAvailable { get; init; }  // When retirement became available
        public double AvgFinalScore { get; init; }
        public double AvgRetiredScore { get; init; }  // Score for those who retired
        public double AvgOustedScore { get; init; }   // Score for those who were ousted
        public double AvgAccumulatedBonus { get; init; }
        public int[] QuarterDistribution { get; init; } = Array.Empty<int>();  // Histogram of game lengths
    }

    private readonly IRng _rng;
    private readonly Strategy _strategy;

    public GameSimulator(IRng rng, Strategy strategy = Strategy.Balanced)
    {
        _rng = rng;
        _strategy = strategy;
    }

    /// <summary>
    /// Runs a single game and returns the result.
    /// </summary>
    public GameResult PlayGame(int seed)
    {
        var rng = new SeededRng(seed);
        var deckSet = GameContent.CreateDeckSet();
        var playableCards = GameContent.PlayableCardDeck;
        var state = QuarterGameState.NewGame(seed, deckSet, playableCards);

        int cardsPlayed = 0;
        int crisesHandled = 0;
        string deathReason = "";
        int quarterRetirementAvailable = 0;
        bool retired = false;

        const int MaxQuarters = 100; // Prevent infinite loops

        while (!state.CEO.IsOusted && !state.CEO.HasRetired && state.Quarter.QuarterNumber <= MaxQuarters)
        {
            try
            {
                // Board Demand phase
                if (state.Quarter.Phase == GamePhase.BoardDemand)
                {
                    var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                    state = newState;
                }

                // Play Cards phase
                while (state.Quarter.Phase == GamePhase.PlayCards)
                {
                    var input = DecideCardPlay(state, rng);
                    var (newState, _) = QuarterEngine.Advance(state, input, rng);

                    if (input.HasPlayedCard) cardsPlayed++;
                    state = newState;
                }

                // Crisis phase
                if (state.Quarter.Phase == GamePhase.Crisis)
                {
                    // First call generates crisis email
                    var (crisisState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                    state = crisisState;

                    // Make crisis choice
                    if (state.CurrentCrisis != null)
                    {
                        var choiceId = DecideCrisisChoice(state, rng);
                        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), rng);
                        state = newState;
                        crisesHandled++;
                    }
                }

                // Resolution phase
                if (state.Quarter.Phase == GamePhase.Resolution)
                {
                    // Decide whether to retire (if eligible)
                    var shouldRetire = DecideRetirement(state, rng);

                    if (shouldRetire && state.CEO.CanRetire)
                    {
                        // Record when retirement first became available
                        if (quarterRetirementAvailable == 0)
                            quarterRetirementAvailable = state.Quarter.QuarterNumber;

                        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.ForRetirement, rng);
                        state = newState;
                        retired = true;
                        deathReason = "Retired";
                    }
                    else
                    {
                        // Track when retirement became available even if we don't retire
                        if (state.CEO.CanRetire && quarterRetirementAvailable == 0)
                            quarterRetirementAvailable = state.Quarter.QuarterNumber;

                        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                        state = newState;

                        if (state.CEO.IsOusted)
                        {
                            deathReason = DetermineDeathReason(state);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                deathReason = $"Error: {ex.Message}";
                break;
            }
        }

        if (state.Quarter.QuarterNumber > MaxQuarters)
        {
            deathReason = "Survived max quarters (victory!)";
        }

        // Calculate final score
        var finalScore = ScoreCalculator.CalculateFinalScore(state.CEO, state.Resources);

        return new GameResult
        {
            QuartersSurvived = state.CEO.QuartersSurvived,
            TotalProfit = state.CEO.TotalProfit,
            FinalFavorability = state.CEO.BoardFavorability,
            FinalEvilScore = state.CEO.EvilScore,
            FinalPC = state.Resources.PoliticalCapital,
            FinalOrg = state.Org,
            CardsPlayed = cardsPlayed,
            CrisesHandled = crisesHandled,
            DeathReason = deathReason,
            UsedStrategy = _strategy,
            Seed = seed,
            Retired = retired,
            AccumulatedBonus = state.CEO.AccumulatedBonus,
            FinalScore = finalScore,
            QuarterRetirementAvailable = quarterRetirementAvailable
        };
    }

    private bool DecideRetirement(QuarterGameState state, IRng rng)
    {
        if (!state.CEO.CanRetire) return false;

        return _strategy switch
        {
            // Conservative: Retire immediately when possible
            Strategy.Conservative => true,

            // Aggressive: Never retire voluntarily, push for max score
            Strategy.Aggressive => false,

            // Balanced: Retire if favorability is getting risky or bonus is high
            Strategy.Balanced => state.CEO.BoardFavorability < 50 ||
                                 state.CEO.AccumulatedBonus >= 70 ||
                                 state.CEO.QuartersSurvived >= 16,

            // Others: 50% chance to retire each quarter once eligible
            _ => rng.NextInt(0, 100) < 50
        };
    }

    private QuarterInput DecideCardPlay(QuarterGameState state, IRng rng)
    {
        var cardsPlayedThisQuarter = state.CardsPlayedThisQuarter.Count;
        var hand = state.Hand.Cards.ToList();

        return _strategy switch
        {
            Strategy.Random => DecideRandom(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.Conservative => DecideConservative(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.Aggressive => DecideAggressive(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.Balanced => DecideBalanced(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.RevenueHunter => DecideRevenueHunter(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.MeterManager => DecideMeterManager(state, hand, cardsPlayedThisQuarter, rng),
            Strategy.DoNothing => QuarterInput.EndCardPlay, // Always skip card play
            Strategy.MinimalEffort => DecideMinimalEffort(state, hand, cardsPlayedThisQuarter, rng),
            _ => QuarterInput.EndCardPlay
        };
    }

    private QuarterInput DecideRandom(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        if (cardsPlayed >= 3 || hand.Count == 0) return QuarterInput.EndCardPlay;

        // 50% chance to end early
        if (rng.NextInt(0, 100) < 50) return QuarterInput.EndCardPlay;

        var card = hand[rng.NextInt(0, hand.Count)];
        return QuarterInput.ForPlayCard(card.CardId, false);
    }

    private QuarterInput DecideConservative(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        // Play at most 1 card, prefer low-risk non-corporate cards
        if (cardsPlayed >= 1) return QuarterInput.EndCardPlay;

        // 30% chance to play nothing (maximum restraint bonus)
        if (rng.NextInt(0, 100) < 30) return QuarterInput.EndCardPlay;

        var safeCards = hand.Where(c => !c.IsCorporate && c.CorporateIntensity == 0).ToList();
        if (safeCards.Count == 0) safeCards = hand;

        var card = safeCards[rng.NextInt(0, safeCards.Count)];
        return QuarterInput.ForPlayCard(card.CardId, true); // End after playing
    }

    private QuarterInput DecideAggressive(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        if (cardsPlayed >= 3 || hand.Count == 0) return QuarterInput.EndCardPlay;
        if (!state.CanAffordNextCard) return QuarterInput.EndCardPlay;

        // Prefer revenue cards and corporate cards
        var preferred = hand
            .OrderByDescending(c => c.Category == CardCategory.Revenue ? 10 : 0)
            .ThenByDescending(c => c.CorporateIntensity)
            .First();

        return QuarterInput.ForPlayCard(preferred.CardId, cardsPlayed >= 2);
    }

    private QuarterInput DecideBalanced(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        // Play 1-2 cards
        if (cardsPlayed >= 2) return QuarterInput.EndCardPlay;
        if (hand.Count == 0) return QuarterInput.EndCardPlay;

        // 20% chance to stop after 1 card
        if (cardsPlayed == 1 && rng.NextInt(0, 100) < 20) return QuarterInput.EndCardPlay;

        // Prefer cards with good affinity and revenue cards
        var bestCard = hand
            .OrderByDescending(c => c.GetAffinityModifier(state.Org))
            .ThenByDescending(c => c.Category == CardCategory.Revenue ? 5 : 0)
            .ThenBy(c => c.CorporateIntensity)
            .First();

        return QuarterInput.ForPlayCard(bestCard.CardId, cardsPlayed >= 1);
    }

    private QuarterInput DecideRevenueHunter(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        if (cardsPlayed >= 3 || hand.Count == 0) return QuarterInput.EndCardPlay;
        if (!state.CanAffordNextCard) return QuarterInput.EndCardPlay;

        // Only play revenue cards
        var revenueCards = hand.Where(c => c.Category == CardCategory.Revenue).ToList();
        if (revenueCards.Count == 0) return QuarterInput.EndCardPlay;

        var card = revenueCards[rng.NextInt(0, revenueCards.Count)];
        return QuarterInput.ForPlayCard(card.CardId, false);
    }

    private QuarterInput DecideMeterManager(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        if (cardsPlayed >= 2) return QuarterInput.EndCardPlay;
        if (hand.Count == 0) return QuarterInput.EndCardPlay;

        // Find lowest meter and play card that helps it
        var lowestMeter = GetLowestMeter(state.Org);

        var helpfulCards = hand
            .Where(c => CardHelpsWithMeter(c, lowestMeter))
            .ToList();

        if (helpfulCards.Count == 0)
        {
            // Play any card with good affinity
            helpfulCards = hand.OrderByDescending(c => c.GetAffinityModifier(state.Org)).Take(2).ToList();
        }

        if (helpfulCards.Count == 0) return QuarterInput.EndCardPlay;

        var card = helpfulCards[rng.NextInt(0, helpfulCards.Count)];
        return QuarterInput.ForPlayCard(card.CardId, cardsPlayed >= 1);
    }

    private Meter GetLowestMeter(OrgState org)
    {
        var meters = new (Meter m, int v)[]
        {
            (Meter.Delivery, org.Delivery),
            (Meter.Morale, org.Morale),
            (Meter.Governance, org.Governance),
            (Meter.Alignment, org.Alignment),
            (Meter.Runway, org.Runway)
        };
        return meters.OrderBy(x => x.v).First().m;
    }

    private bool CardHelpsWithMeter(PlayableCard card, Meter meter)
    {
        // Check if card's expected outcome helps the meter
        foreach (var effect in card.Outcomes.Expected)
        {
            if (effect is MeterEffect me && me.Meter == meter && me.Delta > 0)
                return true;
        }
        return false;
    }

    private QuarterInput DecideMinimalEffort(QuarterGameState state, List<PlayableCard> hand, int cardsPlayed, IRng rng)
    {
        // The exploit: Play exactly 1 revenue card per quarter, then stop
        // This meets profit targets while preserving meters
        if (cardsPlayed >= 1) return QuarterInput.EndCardPlay;
        if (hand.Count == 0) return QuarterInput.EndCardPlay;

        // Find a revenue card - the key to the exploit
        var revenueCards = hand.Where(c => c.Category == CardCategory.Revenue).ToList();
        if (revenueCards.Count > 0)
        {
            // Play the revenue card and immediately end
            var card = revenueCards[rng.NextInt(0, revenueCards.Count)];
            return QuarterInput.ForPlayCard(card.CardId, true); // endAfterThis = true
        }

        // No revenue card? Play any low-risk card to at least do something
        var safeCards = hand.Where(c => c.CorporateIntensity == 0).ToList();
        if (safeCards.Count > 0)
        {
            var card = safeCards[rng.NextInt(0, safeCards.Count)];
            return QuarterInput.ForPlayCard(card.CardId, true);
        }

        // Truly nothing safe? Just end
        return QuarterInput.EndCardPlay;
    }

    private string DecideCrisisChoice(QuarterGameState state, IRng rng)
    {
        var crisis = state.CurrentCrisis!;
        var choices = crisis.Choices;

        return _strategy switch
        {
            Strategy.Conservative => DecideConservativeCrisis(state, choices, rng),
            Strategy.Aggressive => DecideAggressiveCrisis(state, choices, rng),
            _ => DecideBalancedCrisis(state, choices, rng)
        };
    }

    private string DecideConservativeCrisis(QuarterGameState state, IReadOnlyList<Choice> choices, IRng rng)
    {
        // Prefer PC option if affordable, then standard option, avoid corporate
        var pcChoice = choices.FirstOrDefault(c => c.HasPCCost && state.Resources.CanAfford(c.PCCost));
        if (pcChoice != null) return pcChoice.ChoiceId;

        var standardChoice = choices.FirstOrDefault(c => !c.IsCorporateChoice);
        if (standardChoice != null) return standardChoice.ChoiceId;

        return choices[0].ChoiceId;
    }

    private string DecideAggressiveCrisis(QuarterGameState state, IReadOnlyList<Choice> choices, IRng rng)
    {
        // Prefer corporate choice for high risk/reward
        var corporateChoice = choices.FirstOrDefault(c => c.IsCorporateChoice);
        if (corporateChoice != null) return corporateChoice.ChoiceId;

        // Otherwise random
        return choices[rng.NextInt(0, choices.Count)].ChoiceId;
    }

    private string DecideBalancedCrisis(QuarterGameState state, IReadOnlyList<Choice> choices, IRng rng)
    {
        // Use PC if available and evil is getting high, otherwise standard choice
        if (state.CEO.EvilScore >= 5)
        {
            var pcChoice = choices.FirstOrDefault(c => c.HasPCCost && state.Resources.CanAfford(c.PCCost));
            if (pcChoice != null) return pcChoice.ChoiceId;
        }

        // Prefer standard choice
        var standardChoice = choices.FirstOrDefault(c => !c.IsCorporateChoice && !c.HasPCCost);
        if (standardChoice != null) return standardChoice.ChoiceId;

        return choices[0].ChoiceId;
    }

    private string DetermineDeathReason(QuarterGameState state)
    {
        var reasons = new List<string>();

        if (state.CEO.BoardFavorability < 20) reasons.Add("LowFavorability");
        if (state.CEO.EvilScore >= 15) reasons.Add("HighEvil");
        if (state.Org.Runway < 20) reasons.Add("LowRunway");
        if (state.Org.Morale < 20) reasons.Add("LowMorale");
        if (state.Org.Delivery < 20) reasons.Add("LowDelivery");
        if (state.Org.Governance < 20) reasons.Add("LowGovernance");
        if (state.Org.Alignment < 20) reasons.Add("LowAlignment");

        if (reasons.Count == 0) reasons.Add("BoardVote");

        return string.Join("+", reasons);
    }

    /// <summary>
    /// Runs multiple games and compiles statistics.
    /// </summary>
    public static SimulationStats RunSimulation(int gameCount, Strategy strategy, int baseSeed = 42)
    {
        var results = new List<GameResult>();
        var rng = new SeededRng(baseSeed);

        for (int i = 0; i < gameCount; i++)
        {
            var gameSeed = rng.NextInt(0, int.MaxValue);
            var simulator = new GameSimulator(rng, strategy);
            results.Add(simulator.PlayGame(gameSeed));
        }

        var quarters = results.Select(r => r.QuartersSurvived).OrderBy(x => x).ToList();
        var deathReasons = results
            .Where(r => !string.IsNullOrEmpty(r.DeathReason))
            .GroupBy(r => r.DeathReason)
            .ToDictionary(g => g.Key, g => g.Count());

        // Retirement stats
        var retiredGames = results.Where(r => r.Retired).ToList();
        var oustedGames = results.Where(r => !r.Retired).ToList();
        var gamesWithRetirementAvailable = results.Where(r => r.QuarterRetirementAvailable > 0).ToList();

        // Quarter distribution histogram (buckets of 4 quarters = 1 year)
        var maxQ = quarters.Max();
        var bucketCount = (maxQ / 4) + 1;
        var distribution = new int[Math.Max(bucketCount, 8)]; // At least 8 buckets (2 years)
        foreach (var q in quarters)
        {
            var bucket = Math.Min(q / 4, distribution.Length - 1);
            distribution[bucket]++;
        }

        return new SimulationStats
        {
            GamesPlayed = gameCount,
            Strategy = strategy,
            AvgQuartersSurvived = results.Average(r => r.QuartersSurvived),
            MedianQuartersSurvived = quarters[quarters.Count / 2],
            MaxQuartersSurvived = quarters.Max(),
            MinQuartersSurvived = quarters.Min(),
            AvgTotalProfit = results.Average(r => r.TotalProfit),
            AvgFinalFavorability = results.Average(r => r.FinalFavorability),
            AvgEvilScore = results.Average(r => r.FinalEvilScore),
            AvgCardsPerQuarter = results.Where(r => r.QuartersSurvived > 0)
                .Average(r => (double)r.CardsPlayed / r.QuartersSurvived),
            DeathReasons = deathReasons,
            SurvivalRate5Q = (double)results.Count(r => r.QuartersSurvived >= 5) / gameCount * 100,
            SurvivalRate10Q = (double)results.Count(r => r.QuartersSurvived >= 10) / gameCount * 100,
            SurvivalRate20Q = (double)results.Count(r => r.QuartersSurvived >= 20) / gameCount * 100,
            // Retirement stats
            RetirementRate = (double)retiredGames.Count / gameCount * 100,
            AvgRetirementQuarter = retiredGames.Count > 0 ? retiredGames.Average(r => r.QuartersSurvived) : 0,
            AvgQuarterRetirementAvailable = gamesWithRetirementAvailable.Count > 0
                ? gamesWithRetirementAvailable.Average(r => r.QuarterRetirementAvailable) : 0,
            AvgFinalScore = results.Average(r => r.FinalScore),
            AvgRetiredScore = retiredGames.Count > 0 ? retiredGames.Average(r => r.FinalScore) : 0,
            AvgOustedScore = oustedGames.Count > 0 ? oustedGames.Average(r => r.FinalScore) : 0,
            AvgAccumulatedBonus = results.Average(r => r.AccumulatedBonus),
            QuarterDistribution = distribution
        };
    }

    /// <summary>
    /// Formats simulation stats as a readable report.
    /// </summary>
    public static string FormatReport(SimulationStats stats)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Simulation Report: {stats.Strategy} Strategy ===");
        sb.AppendLine($"Games Played: {stats.GamesPlayed}");
        sb.AppendLine();
        sb.AppendLine("--- Game Length ---");
        sb.AppendLine($"Average Quarters: {stats.AvgQuartersSurvived:F1} ({stats.AvgQuartersSurvived / 4:F1} years)");
        sb.AppendLine($"Median Quarters:  {stats.MedianQuartersSurvived}");
        sb.AppendLine($"Range:            {stats.MinQuartersSurvived} - {stats.MaxQuartersSurvived}");
        sb.AppendLine($"Survive 5+ Q:     {stats.SurvivalRate5Q:F1}%");
        sb.AppendLine($"Survive 10+ Q:    {stats.SurvivalRate10Q:F1}%");
        sb.AppendLine($"Survive 20+ Q:    {stats.SurvivalRate20Q:F1}%");
        sb.AppendLine();
        sb.AppendLine("--- Retirement ---");
        sb.AppendLine($"Retirement Rate:       {stats.RetirementRate:F1}%");
        sb.AppendLine($"Avg Retirement Quarter: {stats.AvgRetirementQuarter:F1}");
        sb.AppendLine($"Avg Q Available:       {stats.AvgQuarterRetirementAvailable:F1}");
        sb.AppendLine($"Avg Accumulated Bonus: ${stats.AvgAccumulatedBonus:F0}M");
        sb.AppendLine();
        sb.AppendLine("--- Scores ---");
        sb.AppendLine($"Avg Final Score:    {stats.AvgFinalScore:F0}");
        sb.AppendLine($"Avg Retired Score:  {stats.AvgRetiredScore:F0}");
        sb.AppendLine($"Avg Ousted Score:   {stats.AvgOustedScore:F0}");
        sb.AppendLine();
        sb.AppendLine("--- Performance ---");
        sb.AppendLine($"Avg Total Profit:       ${stats.AvgTotalProfit:F0}M");
        sb.AppendLine($"Avg Final Favorability: {stats.AvgFinalFavorability:F1}%");
        sb.AppendLine($"Avg Evil Score:         {stats.AvgEvilScore:F1}");
        sb.AppendLine($"Avg Cards/Quarter:      {stats.AvgCardsPerQuarter:F2}");
        sb.AppendLine();
        sb.AppendLine("--- Quarter Distribution (by year) ---");
        for (int i = 0; i < stats.QuarterDistribution.Length; i++)
        {
            var count = stats.QuarterDistribution[i];
            var pct = (double)count / stats.GamesPlayed * 100;
            var bar = new string('█', (int)(pct / 2));
            sb.AppendLine($"  Year {i}: {count,4} ({pct,5:F1}%) {bar}");
        }
        sb.AppendLine();
        sb.AppendLine("--- Endings ---");
        foreach (var (reason, count) in stats.DeathReasons.OrderByDescending(x => x.Value))
        {
            var pct = (double)count / stats.GamesPlayed * 100;
            sb.AppendLine($"  {reason}: {count} ({pct:F1}%)");
        }

        return sb.ToString();
    }
}
