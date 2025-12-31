namespace InertiCorp.Core.Tests;

public class OutcomeRollerTests
{
    [Fact]
    public void GetWeights_BaseCase_AfterHoneymoon_Returns20_60_20()
    {
        // After honeymoon period (Q4+), base case is 20/60/20
        var weights = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1, quarterNumber: 4);

        Assert.Equal(20, weights.Good);
        Assert.Equal(60, weights.Expected);
        Assert.Equal(20, weights.Bad);
    }

    [Fact]
    public void GetWeights_HoneymoonPeriod_Q1_GivesBonuses()
    {
        // Q1 gets full honeymoon bonus: +15 good, -10 bad (with fade factor 3/3)
        var weights = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1, quarterNumber: 1);

        Assert.Equal(35, weights.Good);  // 20 + 15
        Assert.Equal(55, weights.Expected);  // 60 - 15 + 10
        Assert.Equal(10, weights.Bad);   // 20 - 10
    }

    [Fact]
    public void GetWeights_HoneymoonPeriod_Q3_GivesReducedBonuses()
    {
        // Q3 gets 1/3 honeymoon bonus: +5 good, -3 bad
        var weights = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1, quarterNumber: 3);

        Assert.Equal(25, weights.Good);  // 20 + 5
        Assert.Equal(58, weights.Expected);  // 100 - 25 - 17
        Assert.Equal(17, weights.Bad);   // 20 - 3
    }

    [Fact]
    public void GetWeights_HighAlignment_ShiftsTowardGood()
    {
        var weightsLow = OutcomeRoller.GetWeights(alignment: 30, pressureLevel: 1);
        var weightsHigh = OutcomeRoller.GetWeights(alignment: 80, pressureLevel: 1);

        Assert.True(weightsHigh.Good > weightsLow.Good);
        Assert.True(weightsHigh.Bad < weightsLow.Bad);
    }

    [Fact]
    public void GetWeights_HighPressure_ShiftsTowardBad()
    {
        var weightsLow = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1);
        var weightsHigh = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 5);

        Assert.True(weightsHigh.Bad > weightsLow.Bad);
        Assert.True(weightsHigh.Good < weightsLow.Good);
    }

    [Fact]
    public void GetWeights_TotalAlways100()
    {
        var testCases = new[]
        {
            (alignment: 10, pressure: 1),
            (alignment: 50, pressure: 3),
            (alignment: 90, pressure: 5),
            (alignment: 30, pressure: 10),
        };

        foreach (var (alignment, pressure) in testCases)
        {
            var weights = OutcomeRoller.GetWeights(alignment, pressure);
            var total = weights.Good + weights.Expected + weights.Bad;
            Assert.Equal(100, total);
        }
    }

    [Fact]
    public void GetWeights_NeverNegative()
    {
        // Test extreme values
        var weights = OutcomeRoller.GetWeights(alignment: 0, pressureLevel: 100);

        Assert.True(weights.Good >= 0);
        Assert.True(weights.Expected >= 0);
        Assert.True(weights.Bad >= 0);
    }

    [Fact]
    public void Roll_UsesCalculatedWeights()
    {
        var profile = new OutcomeProfile(
            Good: Array.Empty<IEffect>(),
            Expected: Array.Empty<IEffect>(),
            Bad: Array.Empty<IEffect>()
        );

        // With very high alignment, should favor Good
        var goodCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var tier = OutcomeRoller.Roll(profile, alignment: 100, pressureLevel: 1, new SeededRng(i));
            if (tier == OutcomeTier.Good) goodCount++;
        }

        Assert.True(goodCount > 25); // Should be above baseline 20%
    }

    [Fact]
    public void Roll_IsDeterministic()
    {
        var profile = new OutcomeProfile(
            Good: Array.Empty<IEffect>(),
            Expected: Array.Empty<IEffect>(),
            Bad: Array.Empty<IEffect>()
        );

        var tier1 = OutcomeRoller.Roll(profile, alignment: 50, pressureLevel: 3, new SeededRng(42));
        var tier2 = OutcomeRoller.Roll(profile, alignment: 50, pressureLevel: 3, new SeededRng(42));

        Assert.Equal(tier1, tier2);
    }

    [Fact]
    public void GetWeights_HighEvilScore_ShiftsTowardBad()
    {
        var weightsLow = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1, evilScore: 0);
        var weightsHigh = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 1, evilScore: 10);

        Assert.True(weightsHigh.Bad > weightsLow.Bad);
        Assert.True(weightsHigh.Good < weightsLow.Good);
    }

    [Fact]
    public void GetWeights_EvilScore_StillTotals100()
    {
        var weights = OutcomeRoller.GetWeights(alignment: 50, pressureLevel: 3, evilScore: 8);
        var total = weights.Good + weights.Expected + weights.Bad;
        Assert.Equal(100, total);
    }
}
