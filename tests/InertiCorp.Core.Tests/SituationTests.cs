using InertiCorp.Core.Content;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Tests;

public class SituationTests
{
    // === SituationTrigger Tests ===

    [Fact]
    public void SituationTrigger_MatchesOutcome_NullMatchesAny()
    {
        var trigger = new SituationTrigger("SIT_TEST", OnOutcome: null, TriggerWeight: 5);

        Assert.True(trigger.MatchesOutcome(OutcomeTier.Good));
        Assert.True(trigger.MatchesOutcome(OutcomeTier.Expected));
        Assert.True(trigger.MatchesOutcome(OutcomeTier.Bad));
    }

    [Fact]
    public void SituationTrigger_MatchesOutcome_SpecificTierOnlyMatches()
    {
        var trigger = new SituationTrigger("SIT_TEST", OnOutcome: OutcomeTier.Bad, TriggerWeight: 5);

        Assert.False(trigger.MatchesOutcome(OutcomeTier.Good));
        Assert.False(trigger.MatchesOutcome(OutcomeTier.Expected));
        Assert.True(trigger.MatchesOutcome(OutcomeTier.Bad));
    }

    // === CardSituations Tests ===

    [Fact]
    public void CardSituations_GetMatchingTriggers_FiltersCorrectly()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_GOOD", OutcomeTier.Good, 5),
            new SituationTrigger("SIT_BAD_1", OutcomeTier.Bad, 4),
            new SituationTrigger("SIT_BAD_2", OutcomeTier.Bad, 3),
            new SituationTrigger("SIT_ANY", null, 2),
        });

        var badTriggers = situations.GetMatchingTriggers(OutcomeTier.Bad);

        Assert.Equal(3, badTriggers.Count); // SIT_BAD_1, SIT_BAD_2, SIT_ANY
        Assert.Contains(badTriggers, t => t.SituationId == "SIT_BAD_1");
        Assert.Contains(badTriggers, t => t.SituationId == "SIT_BAD_2");
        Assert.Contains(badTriggers, t => t.SituationId == "SIT_ANY");
    }

    [Fact]
    public void CardSituations_SelectTrigger_ReturnsNullWhenNoMatches()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_BAD", OutcomeTier.Bad, 5),
        });

        var result = situations.SelectTrigger(OutcomeTier.Good, new SeededRng(42));

        Assert.Null(result);
    }

    [Fact]
    public void CardSituations_SelectTrigger_IsDeterministic()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_1", OutcomeTier.Bad, 5),
            new SituationTrigger("SIT_2", OutcomeTier.Bad, 5),
            new SituationTrigger("SIT_3", OutcomeTier.Bad, 5),
        });

        var result1 = situations.SelectTrigger(OutcomeTier.Bad, new SeededRng(42));
        var result2 = situations.SelectTrigger(OutcomeTier.Bad, new SeededRng(42));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.SituationId, result2.SituationId);
    }

    [Fact]
    public void CardSituations_SelectTrigger_RespectsWeighting()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_RARE", OutcomeTier.Bad, 1),
            new SituationTrigger("SIT_COMMON", OutcomeTier.Bad, 99),
        });

        var commonCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var result = situations.SelectTrigger(OutcomeTier.Bad, new SeededRng(i));
            if (result?.SituationId == "SIT_COMMON") commonCount++;
        }

        // With 99:1 weighting, common should appear most of the time
        Assert.True(commonCount > 80);
    }

    // === PendingSituation Tests ===

    [Fact]
    public void PendingSituation_Create_SetsCorrectFields()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 2, delayQuarters: 3);

        Assert.Equal("SIT_TEST", pending.SituationId);
        Assert.Equal("CARD_TEST", pending.OriginCardId);
        Assert.Equal(5, pending.ScheduledQuarter); // 2 + 3
        Assert.Equal(2, pending.QueuedAtQuarter);
        Assert.Equal(0, pending.DeferCount);
    }

    [Fact]
    public void PendingSituation_IsDueAt_TrueWhenQuarterReached()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 2);

        Assert.False(pending.IsDueAt(1)); // Scheduled for Q3
        Assert.False(pending.IsDueAt(2));
        Assert.True(pending.IsDueAt(3));
        Assert.True(pending.IsDueAt(4)); // Still ready even after
    }

    [Fact]
    public void PendingSituation_QuartersWaiting_CalculatesCorrectly()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0);

        Assert.Equal(0, pending.QuartersWaiting(1)); // Immediate
        Assert.Equal(1, pending.QuartersWaiting(2));
        Assert.Equal(2, pending.QuartersWaiting(3));
    }

    [Fact]
    public void PendingSituation_WithDeferred_IncrementsDeferCount()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0);

        var deferred = pending.WithDeferred(currentQuarter: 2);

        Assert.Equal(1, deferred.DeferCount);
        Assert.Equal(1, deferred.QueuedAtQuarter); // QueuedAtQuarter stays original
    }

    [Fact]
    public void PendingSituation_WithDeferred_MultipleTimes()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0);

        var deferred1 = pending.WithDeferred(currentQuarter: 2);
        var deferred2 = deferred1.WithDeferred(currentQuarter: 3);
        var deferred3 = deferred2.WithDeferred(currentQuarter: 4);

        Assert.Equal(3, deferred3.DeferCount);
    }

    // === SituationResolver Tests ===

    [Fact]
    public void SituationResolver_CheckForTrigger_ReturnsNullOnHighRoll()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_TEST", OutcomeTier.Bad, 10),
        });

        // Find a seed that produces a high roll (18-20 = no trigger)
        var nullCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var result = SituationResolver.CheckForTrigger(situations, OutcomeTier.Bad, currentQuarter: 1, new SeededRng(i));
            if (result == null) nullCount++;
        }

        // About 15% (3/20) should be null
        Assert.True(nullCount >= 5 && nullCount <= 30);
    }

    [Fact]
    public void SituationResolver_CheckForTrigger_IsDeterministic()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_TEST", OutcomeTier.Bad, 10),
        });

        var result1 = SituationResolver.CheckForTrigger(situations, OutcomeTier.Bad, currentQuarter: 1, new SeededRng(42));
        var result2 = SituationResolver.CheckForTrigger(situations, OutcomeTier.Bad, currentQuarter: 1, new SeededRng(42));

        Assert.Equal(result1?.SituationId, result2?.SituationId);
        Assert.Equal(result1?.ScheduledQuarter, result2?.ScheduledQuarter);
    }

    [Fact]
    public void SituationResolver_CheckForTrigger_DelayDistribution()
    {
        var situations = new CardSituations("TEST_CARD", new[]
        {
            new SituationTrigger("SIT_TEST", null, 10), // Matches any outcome
        });

        var delays = new Dictionary<int, int> { [0] = 0, [1] = 0, [2] = 0, [3] = 0 };

        for (int i = 0; i < 1000; i++)
        {
            var result = SituationResolver.CheckForTrigger(situations, OutcomeTier.Expected, currentQuarter: 1, new SeededRng(i));
            if (result != null)
            {
                var delay = result.ScheduledQuarter - 1;
                if (delays.ContainsKey(delay)) delays[delay]++;
            }
        }

        // Verify distribution roughly matches: 5/17 immediate, 5/17 +1, 4/17 +2, 3/17 +3
        // (17 because 3/20 are no-trigger)
        Assert.True(delays[0] > 150, $"Immediate triggers: {delays[0]}");
        Assert.True(delays[1] > 150, $"+1 quarter triggers: {delays[1]}");
        Assert.True(delays[2] > 100, $"+2 quarter triggers: {delays[2]}");
        Assert.True(delays[3] > 50, $"+3 quarter triggers: {delays[3]}");
    }

    [Fact]
    public void SituationResolver_CheckDecay_ImmediateAlwaysFires()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0);

        for (int i = 0; i < 100; i++)
        {
            var fires = SituationResolver.CheckDecay(pending, currentQuarter: 1, new SeededRng(i));
            Assert.True(fires);
        }
    }

    [Fact]
    public void SituationResolver_CheckDecay_DecaysProbabilityOverTime()
    {
        // Test that longer waits have lower survival rates
        var fireRates = new Dictionary<int, int>();

        for (int quartersWaiting = 1; quartersWaiting <= 4; quartersWaiting++)
        {
            var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0);
            var fires = 0;

            for (int i = 0; i < 100; i++)
            {
                if (SituationResolver.CheckDecay(pending, currentQuarter: 1 + quartersWaiting, new SeededRng(i)))
                    fires++;
            }
            fireRates[quartersWaiting] = fires;
        }

        // Each successive quarter should have lower fire rate
        Assert.True(fireRates[1] > fireRates[2], $"Q1: {fireRates[1]}, Q2: {fireRates[2]}");
        Assert.True(fireRates[2] > fireRates[3], $"Q2: {fireRates[2]}, Q3: {fireRates[3]}");
        Assert.True(fireRates[3] > fireRates[4], $"Q3: {fireRates[3]}, Q4: {fireRates[4]}");
    }

    [Fact]
    public void SituationResolver_CheckResurface_Has30PercentChance()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0)
            .WithDeferred(currentQuarter: 1);

        var resurfaceCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (SituationResolver.CheckResurface(pending, new SeededRng(i)))
                resurfaceCount++;
        }

        // Should be around 30% (allow 25-35% for statistical variance)
        Assert.True(resurfaceCount >= 250 && resurfaceCount <= 350, $"Resurface count: {resurfaceCount}");
    }

    [Fact]
    public void SituationResolver_ShouldFade_AfterFourQuarters()
    {
        var pending = PendingSituation.Create("SIT_TEST", "CARD_TEST", currentQuarter: 1, delayQuarters: 0)
            .WithDeferred(currentQuarter: 1);

        Assert.False(SituationResolver.ShouldFade(pending, currentQuarter: 2));
        Assert.False(SituationResolver.ShouldFade(pending, currentQuarter: 3));
        Assert.False(SituationResolver.ShouldFade(pending, currentQuarter: 4));
        Assert.True(SituationResolver.ShouldFade(pending, currentQuarter: 5)); // 4 quarters after Q1
    }

    [Fact]
    public void SituationResolver_RollResponseOutcome_PCHasHighGoodRate()
    {
        var goodCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var outcome = SituationResolver.RollResponseOutcome(ResponseType.PC, new SeededRng(i));
            if (outcome == OutcomeTier.Good) goodCount++;
        }

        // PC has 70% good rate
        Assert.True(goodCount >= 55 && goodCount <= 85, $"Good count: {goodCount}");
    }

    [Fact]
    public void SituationResolver_RollResponseOutcome_RiskHasBalancedRates()
    {
        var goodCount = 0;
        var expectedCount = 0;
        var badCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            var outcome = SituationResolver.RollResponseOutcome(ResponseType.Risk, new SeededRng(i));
            switch (outcome)
            {
                case OutcomeTier.Good: goodCount++; break;
                case OutcomeTier.Expected: expectedCount++; break;
                case OutcomeTier.Bad: badCount++; break;
            }
        }

        // Risk has 40/40/20 distribution
        Assert.True(goodCount >= 300 && goodCount <= 500, $"Good: {goodCount}");
        Assert.True(expectedCount >= 300 && expectedCount <= 500, $"Expected: {expectedCount}");
        Assert.True(badCount >= 100 && badCount <= 300, $"Bad: {badCount}");
    }

    [Fact]
    public void SituationResolver_ResolveResponse_DeferReturnsNoEffects()
    {
        var situation = SituationContent.All.Values.First();

        var result = SituationResolver.ResolveResponse(situation, ResponseType.Defer, new SeededRng(42));

        Assert.True(result.WasDeferred);
        Assert.Empty(result.Effects);
        Assert.Equal(0, result.EvilDelta);
        Assert.Equal(0, result.PCSpent);
    }

    // === SituationDefinition Tests ===

    [Fact]
    public void SituationDefinition_CanDefer_FalseForCritical()
    {
        var minor = new SituationDefinition(
            "SIT_MINOR", "Test", "Desc", "Subject", "Body",
            SituationSeverity.Minor, Array.Empty<SituationResponse>());

        var critical = new SituationDefinition(
            "SIT_CRITICAL", "Test", "Desc", "Subject", "Body",
            SituationSeverity.Critical, Array.Empty<SituationResponse>());

        Assert.True(minor.CanDefer);
        Assert.False(critical.CanDefer);
    }

    [Fact]
    public void SituationDefinition_WithEscalatedSeverity_IncreasesLevel()
    {
        var minor = new SituationDefinition(
            "SIT_TEST", "Test", "Desc", "Subject", "Body",
            SituationSeverity.Minor, Array.Empty<SituationResponse>());

        var escalated = minor.WithEscalatedSeverity();

        Assert.Equal(SituationSeverity.Moderate, escalated.Severity);
    }

    [Fact]
    public void SituationDefinition_WithEscalatedSeverity_CapsAtCritical()
    {
        var major = new SituationDefinition(
            "SIT_TEST", "Test", "Desc", "Subject", "Body",
            SituationSeverity.Major, Array.Empty<SituationResponse>());

        var escalated1 = major.WithEscalatedSeverity();
        var escalated2 = escalated1.WithEscalatedSeverity();

        Assert.Equal(SituationSeverity.Critical, escalated1.Severity);
        Assert.Equal(SituationSeverity.Critical, escalated2.Severity); // Can't go beyond Critical
    }

    // === SituationResponse Tests ===

    [Fact]
    public void SituationResponse_GetWeightsForType_PCHasHighSuccess()
    {
        var weights = SituationResponse.GetWeightsForType(ResponseType.PC);

        Assert.Equal(70, weights.Good);
        Assert.Equal(20, weights.Expected);
        Assert.Equal(10, weights.Bad);
    }

    [Fact]
    public void SituationResponse_GetWeightsForType_AllSumTo100()
    {
        foreach (ResponseType type in Enum.GetValues<ResponseType>())
        {
            var weights = SituationResponse.GetWeightsForType(type);
            var total = weights.Good + weights.Expected + weights.Bad;
            Assert.Equal(100, total);
        }
    }

    // === Content Validation Tests ===

    [Fact]
    public void SituationContent_AllSituationsHaveValidStructure()
    {
        foreach (var situation in SituationContent.All.Values)
        {
            Assert.False(string.IsNullOrEmpty(situation.SituationId));
            Assert.False(string.IsNullOrEmpty(situation.Title));
            Assert.False(string.IsNullOrEmpty(situation.EmailSubject));
            Assert.False(string.IsNullOrEmpty(situation.EmailBody));
            Assert.True(situation.Responses.Count >= 3); // At least PC, Risk, Evil (Defer is added automatically)
        }
    }

    [Fact]
    public void SituationContent_AllSituationsHaveAllResponseTypes()
    {
        foreach (var situation in SituationContent.All.Values)
        {
            var types = situation.Responses.Select(r => r.Type).ToHashSet();

            Assert.Contains(ResponseType.PC, types);
            Assert.Contains(ResponseType.Risk, types);
            Assert.Contains(ResponseType.Evil, types);
        }
    }

    [Fact]
    public void CardSituationMappings_AllMappedCardsHaveValidSituationIds()
    {
        var validSituationIds = SituationContent.All.Keys.ToHashSet();

        foreach (var mapping in CardSituationMappings.All.Values)
        {
            foreach (var trigger in mapping.PossibleSituations)
            {
                Assert.Contains(trigger.SituationId, validSituationIds);
            }
        }
    }

    [Fact]
    public void CardSituationMappings_AllTriggersHavePositiveWeights()
    {
        foreach (var mapping in CardSituationMappings.All.Values)
        {
            foreach (var trigger in mapping.PossibleSituations)
            {
                Assert.True(trigger.TriggerWeight > 0, $"Trigger {trigger.SituationId} has non-positive weight");
            }
        }
    }
}
