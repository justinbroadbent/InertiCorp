using InertiCorp.Core;
using InertiCorp.Core.Content;

namespace InertiCorp.Core.Tests;

/// <summary>
/// Balance tests that run game simulations to gather metrics.
/// Run with: dotnet test --filter "BalanceTests" --verbosity normal
/// </summary>
public class BalanceTests
{
    [Fact]
    public void SimulateAllStrategies()
    {
        const int GamesPerStrategy = 500;

        var strategies = new[]
        {
            GameSimulator.Strategy.Conservative,
            GameSimulator.Strategy.Balanced,
            GameSimulator.Strategy.Aggressive,
            GameSimulator.Strategy.RevenueHunter,
            GameSimulator.Strategy.MeterManager,
            GameSimulator.Strategy.Random,
            GameSimulator.Strategy.DoNothing,      // Test anti-coasting mechanics
            GameSimulator.Strategy.MinimalEffort   // Test single-revenue-card exploit
        };

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("INERTICORP BALANCE SIMULATION");
        Console.WriteLine($"Running {GamesPerStrategy} games per strategy");
        Console.WriteLine(new string('=', 60) + "\n");

        var allStats = new List<GameSimulator.SimulationStats>();

        foreach (var strategy in strategies)
        {
            var stats = GameSimulator.RunSimulation(GamesPerStrategy, strategy);
            allStats.Add(stats);
            Console.WriteLine(GameSimulator.FormatReport(stats));
            Console.WriteLine();
        }

        // Summary comparison
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STRATEGY COMPARISON SUMMARY");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"Strategy",-15} {"Avg Q",-8} {"Retire%",-10} {"Avg Score",-12} {"Retire Q",-10}");
        Console.WriteLine(new string('-', 80));
        foreach (var s in allStats.OrderByDescending(x => x.RetirementRate))
        {
            Console.WriteLine($"{s.Strategy,-15} {s.AvgQuartersSurvived,-8:F1} {s.RetirementRate,-10:F1} {s.AvgFinalScore,-12:F0} {s.AvgRetirementQuarter,-10:F1}");
        }

        // Retirement analysis
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("RETIREMENT ANALYSIS");
        Console.WriteLine(new string('=', 80));
        var avgRetirementRate = allStats.Average(s => s.RetirementRate);
        var avgRetirementQ = allStats.Where(s => s.AvgRetirementQuarter > 0).Average(s => s.AvgRetirementQuarter);
        var avgAvailableQ = allStats.Where(s => s.AvgQuarterRetirementAvailable > 0).Average(s => s.AvgQuarterRetirementAvailable);
        Console.WriteLine($"Overall retirement rate: {avgRetirementRate:F1}%");
        Console.WriteLine($"Average retirement quarter: {avgRetirementQ:F1} ({avgRetirementQ / 4:F1} years)");
        Console.WriteLine($"Retirement typically available at: Q{avgAvailableQ:F1}");
        Console.WriteLine($"Average accumulated bonus: ${allStats.Average(s => s.AvgAccumulatedBonus):F0}M");
        Console.WriteLine();
        Console.WriteLine($"{"Strategy",-15} {"Retired Scr",-12} {"Ousted Scr",-12} {"Score Ratio",-12}");
        Console.WriteLine(new string('-', 60));
        foreach (var s in allStats.OrderByDescending(x => x.RetirementRate))
        {
            var scoreRatio = s.AvgOustedScore > 0 ? s.AvgRetiredScore / s.AvgOustedScore : 0;
            Console.WriteLine($"{s.Strategy,-15} {s.AvgRetiredScore,-12:F0} {s.AvgOustedScore,-12:F0} {scoreRatio,-12:F1}x");
        }

        // Aggregate death reasons
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("AGGREGATE DEATH REASONS (all strategies)");
        Console.WriteLine(new string('=', 60));
        var allDeaths = allStats
            .SelectMany(s => s.DeathReasons)
            .GroupBy(x => x.Key)
            .Select(g => (Reason: g.Key, Count: g.Sum(x => x.Value)))
            .OrderByDescending(x => x.Count)
            .ToList();

        int totalGames = allStats.Sum(s => s.GamesPlayed);
        foreach (var (reason, count) in allDeaths)
        {
            var pct = (double)count / totalGames * 100;
            Console.WriteLine($"  {reason}: {count} ({pct:F1}%)");
        }

        // Balance recommendations
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("BALANCE ANALYSIS");
        Console.WriteLine(new string('=', 60));

        var avgSurvival = allStats.Average(s => s.AvgQuartersSurvived);
        Console.WriteLine($"Overall average survival: {avgSurvival:F1} quarters");

        if (avgSurvival < 5)
        {
            Console.WriteLine("WARNING: Game is too hard! Players die too quickly.");
            Console.WriteLine("Recommendations:");
            Console.WriteLine("  - Increase starting favorability");
            Console.WriteLine("  - Reduce board pressure scaling");
            Console.WriteLine("  - Extend honeymoon period");
            Console.WriteLine("  - Increase good outcome probabilities");
        }
        else if (avgSurvival > 20)
        {
            Console.WriteLine("WARNING: Game might be too easy.");
            Console.WriteLine("Recommendations:");
            Console.WriteLine("  - Increase board pressure");
            Console.WriteLine("  - Add more impactful crisis penalties");
        }
        else
        {
            Console.WriteLine("Balance seems reasonable.");
        }

        // Check for dominant strategies
        var best = allStats.OrderByDescending(s => s.AvgQuartersSurvived).First();
        var worst = allStats.OrderBy(s => s.AvgQuartersSurvived).First();
        var ratio = best.AvgQuartersSurvived / worst.AvgQuartersSurvived;

        Console.WriteLine($"\nBest strategy:  {best.Strategy} ({best.AvgQuartersSurvived:F1} Q avg)");
        Console.WriteLine($"Worst strategy: {worst.Strategy} ({worst.AvgQuartersSurvived:F1} Q avg)");
        Console.WriteLine($"Ratio: {ratio:F2}x");

        if (ratio > 3)
        {
            Console.WriteLine("WARNING: Strategy imbalance detected. Some strategies are much stronger.");
        }

        // Assert some basic expectations
        Assert.True(avgSurvival >= 1, "Games should last at least 1 quarter on average");
    }

    [Fact]
    public void DetailedBalancedStrategyAnalysis()
    {
        const int GameCount = 200;

        var stats = GameSimulator.RunSimulation(GameCount, GameSimulator.Strategy.Balanced);
        Console.WriteLine(GameSimulator.FormatReport(stats));

        // The balanced strategy should be viable
        Assert.True(stats.AvgQuartersSurvived >= 3, $"Balanced strategy should survive at least 3 quarters on average, got {stats.AvgQuartersSurvived:F1}");
        Assert.True(stats.SurvivalRate5Q >= 20, $"At least 20% should survive 5+ quarters, got {stats.SurvivalRate5Q:F1}%");
    }

    [Fact]
    public void MeterDecayAnalysis()
    {
        // Test how quickly meters decay without intervention
        var rng = new SeededRng(12345);
        var deckSet = GameContent.CreateDeckSet();
        var playableCards = GameContent.PlayableCardDeck;
        var state = QuarterGameState.NewGame(12345, deckSet, playableCards);

        Console.WriteLine("\n=== Meter Decay Analysis (Playing 0 cards each quarter) ===\n");
        Console.WriteLine($"{"Q",-4} {"Del",-6} {"Mor",-6} {"Gov",-6} {"Ali",-6} {"Run",-6} {"Fav",-6} {"Evil",-6}");
        Console.WriteLine(new string('-', 52));

        for (int q = 1; q <= 12 && !state.CEO.IsOusted; q++)
        {
            Console.WriteLine($"{q,-4} {state.Org.Delivery,-6} {state.Org.Morale,-6} {state.Org.Governance,-6} {state.Org.Alignment,-6} {state.Org.Runway,-6} {state.CEO.BoardFavorability,-6} {state.CEO.EvilScore,-6}");

            // Board Demand
            if (state.Quarter.Phase == GamePhase.BoardDemand)
            {
                var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                state = newState;
            }

            // Skip card play (end immediately)
            if (state.Quarter.Phase == GamePhase.PlayCards)
            {
                var (newState, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, rng);
                state = newState;
            }

            // Crisis - pick standard option
            if (state.Quarter.Phase == GamePhase.Crisis)
            {
                var (crisisState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                state = crisisState;

                if (state.CurrentCrisis != null)
                {
                    var standardChoice = state.CurrentCrisis.Choices
                        .FirstOrDefault(c => !c.IsCorporateChoice && !c.HasPCCost)
                        ?? state.CurrentCrisis.Choices[0];
                    var (newState, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(standardChoice.ChoiceId), rng);
                    state = newState;
                }
            }

            // Resolution
            if (state.Quarter.Phase == GamePhase.Resolution)
            {
                var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
                state = newState;
            }
        }

        Console.WriteLine($"\nFinal state: {(state.CEO.IsOusted ? "OUSTED" : "Survived")} after {state.CEO.QuartersSurvived} quarters");
    }

    [Fact]
    public void EarlyGameSurvivalAnalysis()
    {
        // Focus on first 5 quarters - where most deaths seem to happen
        const int GameCount = 300;
        var rng = new SeededRng(99999);

        var earlyDeaths = new List<(int Quarter, string Reason, int Favorability, OrgState Org)>();

        for (int i = 0; i < GameCount; i++)
        {
            var seed = rng.NextInt(0, int.MaxValue);
            var gameRng = new SeededRng(seed);
            var deckSet = GameContent.CreateDeckSet();
            var playableCards = GameContent.PlayableCardDeck;
            var state = QuarterGameState.NewGame(seed, deckSet, playableCards);

            while (!state.CEO.IsOusted && state.Quarter.QuarterNumber <= 5)
            {
                try
                {
                    // Simple balanced play
                    if (state.Quarter.Phase == GamePhase.BoardDemand)
                    {
                        var (ns, _) = QuarterEngine.Advance(state, QuarterInput.Empty, gameRng);
                        state = ns;
                    }

                    if (state.Quarter.Phase == GamePhase.PlayCards)
                    {
                        // Play 1-2 cards
                        for (int c = 0; c < 2 && state.Quarter.Phase == GamePhase.PlayCards && state.Hand.Count > 0; c++)
                        {
                            var card = state.Hand.Cards.First();
                            var (ns, _) = QuarterEngine.Advance(state, QuarterInput.ForPlayCard(card.CardId, c >= 1), gameRng);
                            state = ns;
                        }
                        if (state.Quarter.Phase == GamePhase.PlayCards)
                        {
                            var (ns, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, gameRng);
                            state = ns;
                        }
                    }

                    if (state.Quarter.Phase == GamePhase.Crisis)
                    {
                        var (cs, _) = QuarterEngine.Advance(state, QuarterInput.Empty, gameRng);
                        state = cs;
                        if (state.CurrentCrisis != null)
                        {
                            var choice = state.CurrentCrisis.Choices
                                .FirstOrDefault(c => !c.IsCorporateChoice) ?? state.CurrentCrisis.Choices[0];
                            var (ns, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choice.ChoiceId), gameRng);
                            state = ns;
                        }
                    }

                    if (state.Quarter.Phase == GamePhase.Resolution)
                    {
                        var (ns, _) = QuarterEngine.Advance(state, QuarterInput.Empty, gameRng);
                        state = ns;
                    }
                }
                catch { break; }
            }

            if (state.CEO.IsOusted && state.CEO.QuartersSurvived <= 5)
            {
                var reason = $"Fav:{state.CEO.BoardFavorability} Evil:{state.CEO.EvilScore}";
                earlyDeaths.Add((state.CEO.QuartersSurvived, reason, state.CEO.BoardFavorability, state.Org));
            }
        }

        Console.WriteLine($"\n=== Early Game Analysis (First 5 Quarters) ===");
        Console.WriteLine($"Games: {GameCount}");
        Console.WriteLine($"Early deaths (Q1-5): {earlyDeaths.Count} ({100.0 * earlyDeaths.Count / GameCount:F1}%)");

        if (earlyDeaths.Count > 0)
        {
            Console.WriteLine($"\nDeaths by quarter:");
            for (int q = 1; q <= 5; q++)
            {
                var deathsThisQ = earlyDeaths.Count(d => d.Quarter == q);
                Console.WriteLine($"  Q{q}: {deathsThisQ} ({100.0 * deathsThisQ / GameCount:F1}%)");
            }

            Console.WriteLine($"\nAverage favorability at death: {earlyDeaths.Average(d => d.Favorability):F1}");

            var avgOrg = new
            {
                Delivery = earlyDeaths.Average(d => d.Org.Delivery),
                Morale = earlyDeaths.Average(d => d.Org.Morale),
                Governance = earlyDeaths.Average(d => d.Org.Governance),
                Alignment = earlyDeaths.Average(d => d.Org.Alignment),
                Runway = earlyDeaths.Average(d => d.Org.Runway)
            };
            Console.WriteLine($"Average org state at death:");
            Console.WriteLine($"  Delivery: {avgOrg.Delivery:F1}");
            Console.WriteLine($"  Morale: {avgOrg.Morale:F1}");
            Console.WriteLine($"  Governance: {avgOrg.Governance:F1}");
            Console.WriteLine($"  Alignment: {avgOrg.Alignment:F1}");
            Console.WriteLine($"  Runway: {avgOrg.Runway:F1}");
        }

        // This is informational, not a hard assertion
        Assert.True(true);
    }
}
