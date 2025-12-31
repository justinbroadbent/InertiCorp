namespace InertiCorp.Core.Tests;

public class FavorabilityCalculatorTests
{
    [Fact]
    public void Calculate_ProfitUpAndDirectiveMet_RewardsSmall()
    {
        var lastProfit = 100;
        var currentProfit = 120;
        var directiveMet = true;
        var pressureLevel = 1;

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        Assert.Equal(8, change); // +8 for success (rebalanced from 5)
    }

    [Fact]
    public void Calculate_ProfitFlat_DirectiveMet_PartialSuccess()
    {
        var lastProfit = 100;
        var currentProfit = 100; // Flat but positive
        var directiveMet = true;
        var pressureLevel = 1;

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        // Positive profit + directive met = partial success (half reward)
        Assert.Equal(4, change); // SuccessReward / 2 = 8 / 2 = 4
    }

    [Fact]
    public void Calculate_DirectiveFailed_AdditionalPenalty()
    {
        var lastProfit = 100;
        var currentProfit = 105; // Small increase but directive failed
        var directiveMet = false;
        var pressureLevel = 1;

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        // -4 for directive failure + -1 for pressure (full pressure now, not halved)
        Assert.Equal(-5, change);
    }

    [Fact]
    public void Calculate_ProfitDownAndDirectiveFailed_CombinedPenalties()
    {
        var lastProfit = 100;
        var currentProfit = 90; // Down 10 (moderate decline)
        var directiveMet = false;
        var pressureLevel = 1;

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        // -3 for decline + -4 for directive + -1 for pressure (full now, not halved) = -8
        Assert.Equal(-8, change);
    }

    [Fact]
    public void Calculate_HigherPressure_IncreasesFailurePenalty()
    {
        var lastProfit = 100;
        var currentProfit = 90;
        var directiveMet = false;

        var changeP1 = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel: 1);
        var changeP5 = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel: 5);

        Assert.True(changeP5 < changeP1); // Higher pressure = more penalty
    }

    [Fact]
    public void Calculate_PressureScaling_AppliesOnFailures()
    {
        var lastProfit = 100;
        var currentProfit = 120;
        var directiveMet = false; // Failed directive
        var pressureLevel = 3;

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        // Base -5 for directive, plus -3 for pressure (rebalanced from -10)
        Assert.True(change <= -5);
    }

    [Fact]
    public void Calculate_Success_NoPressurePenalty()
    {
        var lastProfit = 100;
        var currentProfit = 150;
        var directiveMet = true;
        var pressureLevel = 5; // High pressure but success

        var change = FavorabilityCalculator.Calculate(lastProfit, currentProfit, directiveMet, pressureLevel);

        Assert.Equal(8, change); // No pressure penalty on success (rebalanced from 5)
    }

    [Fact]
    public void Calculate_WeakProjectStreak_Q1_SmallPenalty()
    {
        var change = FavorabilityCalculator.Calculate(
            lastProfit: 100, currentProfit: 120, directiveMet: true, pressureLevel: 1,
            evilScore: 0, weakProjectStreak: 1);

        // Full success (+8) minus weak project Q1 penalty (-1) = +7, capped at 6
        Assert.Equal(6, change);
    }

    [Fact]
    public void Calculate_WeakProjectStreak_Q2_ModerateImpact()
    {
        var change = FavorabilityCalculator.Calculate(
            lastProfit: 100, currentProfit: 120, directiveMet: true, pressureLevel: 1,
            evilScore: 0, weakProjectStreak: 2);

        // Full success (+8) minus Q2 penalty (-3) = +5, but capped at +2
        Assert.Equal(2, change);
    }

    [Fact]
    public void Calculate_WeakProjectStreak_Q3_CapsPositiveGainsAtZero()
    {
        var change = FavorabilityCalculator.Calculate(
            lastProfit: 100, currentProfit: 120, directiveMet: true, pressureLevel: 1,
            evilScore: 0, weakProjectStreak: 3);

        // Full success (+8) minus Q3 penalty (-5) = +3, but capped at 0
        Assert.Equal(0, change);
    }

    [Fact]
    public void Calculate_WeakProjectStreak_Q4Plus_SeverePenalty()
    {
        var change = FavorabilityCalculator.Calculate(
            lastProfit: 100, currentProfit: 120, directiveMet: true, pressureLevel: 1,
            evilScore: 0, weakProjectStreak: 4);

        // Full success (+8) minus Q4+ penalty (-7) = +1, but capped at 0
        Assert.Equal(0, change);
    }

    [Fact]
    public void Calculate_WeakProjectStreak_CompoundsWithFailure()
    {
        var change = FavorabilityCalculator.Calculate(
            lastProfit: 100, currentProfit: 90, directiveMet: false, pressureLevel: 1,
            evilScore: 0, weakProjectStreak: 3);

        // Decline penalty (-3) + directive failed (-4) + Q3 streak (-5) = -12 (hits cap)
        Assert.Equal(-12, change);
    }

    [Fact]
    public void GetLowMeterAdjustment_SingleCriticalMeter_CapsGainsAndAddsSmallPenalty()
    {
        var org = new OrgState(Delivery: 4, Morale: 50, Governance: 50, Alignment: 50, Runway: 50);
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowMeterAdjustment(org);

        Assert.Equal(0, maxGain); // No positive gains allowed
        Assert.Equal(-2, penalty); // Small penalty
        Assert.Contains("Delivery", reason!);
        Assert.Contains("critically low", reason!);
    }

    [Fact]
    public void GetLowMeterAdjustment_TwoCriticalMeters_SeverePenaltyAndNoGains()
    {
        var org = new OrgState(Delivery: 50, Morale: 0, Governance: 50, Alignment: 50, Runway: 3);
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowMeterAdjustment(org);

        Assert.Equal(0, maxGain); // No positive gains
        Assert.Equal(-5, penalty); // Severe penalty
        Assert.Contains("crisis", reason!);
        Assert.Contains("Morale", reason!);
        Assert.Contains("Runway", reason!);
    }

    [Fact]
    public void GetLowMeterAdjustment_MultipleLowButNotCritical_CapsGains()
    {
        var org = new OrgState(Delivery: 10, Morale: 12, Governance: 8, Alignment: 50, Runway: 50);
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowMeterAdjustment(org);

        Assert.Equal(2, maxGain); // Capped at 2
        Assert.Equal(0, penalty); // No penalty for low-but-not-critical
    }

    [Fact]
    public void GetLowMeterAdjustment_HealthyMeters_NoRestrictions()
    {
        var org = new OrgState(Delivery: 50, Morale: 50, Governance: 50, Alignment: 50, Runway: 50);
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowMeterAdjustment(org);

        Assert.Equal(int.MaxValue, maxGain); // No cap
        Assert.Equal(0, penalty); // No penalty
        Assert.Null(reason);
    }

    [Fact]
    public void GetExpectedProjectCount_Q1Q2_ExpectsOneProject()
    {
        Assert.Equal(1, FavorabilityCalculator.GetExpectedProjectCount(0)); // Q1
        Assert.Equal(1, FavorabilityCalculator.GetExpectedProjectCount(1)); // Q2
    }

    [Fact]
    public void GetExpectedProjectCount_Q3Plus_ExpectsTwoProjects()
    {
        Assert.Equal(2, FavorabilityCalculator.GetExpectedProjectCount(2)); // Q3
        Assert.Equal(2, FavorabilityCalculator.GetExpectedProjectCount(3)); // Q4
        Assert.Equal(2, FavorabilityCalculator.GetExpectedProjectCount(8)); // Q9
        Assert.Equal(2, FavorabilityCalculator.GetExpectedProjectCount(12)); // Q13
    }

    [Fact]
    public void GetLowActivityAdjustment_Q1Q2_NoPenaltyForSingleProject()
    {
        // Q1-Q2: 1 project is fine (honeymoon)
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(1, 0);
        Assert.Equal(int.MaxValue, maxGain);
        Assert.Equal(0, penalty);
        Assert.Null(reason);

        // Q2 still honeymoon
        (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(1, 1);
        Assert.Equal(int.MaxValue, maxGain);
        Assert.Equal(0, penalty);
    }

    [Fact]
    public void GetLowActivityAdjustment_Q3Plus_PenaltyForSingleProject()
    {
        // Q3: Board expects 2 projects, 1 is below expectations
        // tenureMultiplier = 1 + (2/3) = 1, penalty = -4 * 1 = -4
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(1, 2);

        Assert.Equal(0, maxGain); // No positive gains allowed
        Assert.Equal(-4, penalty); // Scales with tenure
        Assert.Contains("expected 2", reason!);
    }

    [Fact]
    public void GetLowActivityAdjustment_Q3Plus_NoPenaltyForTwoProjects()
    {
        // Q3+: 2 projects meets expectations
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(2, 2);
        Assert.Equal(int.MaxValue, maxGain);
        Assert.Equal(0, penalty);
        Assert.Null(reason);
    }

    [Fact]
    public void GetLowActivityAdjustment_NoProjects_SeverePenalty()
    {
        // No projects at all in Q5 = severe penalty
        // tenureMultiplier = 1 + (4/3) = 2, penalty = -5 * 2 = -10
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(0, 4);

        Assert.Equal(0, maxGain); // No positive gains
        Assert.Equal(-10, penalty); // Scales with tenure
        Assert.Contains("active strategic leadership", reason!);
    }

    [Fact]
    public void GetLowActivityAdjustment_LateGame_PenaltyScalesHarshly()
    {
        // Q12: tenureMultiplier = 1 + (12/3) = 5, penalty = -4 * 5 = -20
        var (maxGain, penalty, reason) = FavorabilityCalculator.GetLowActivityAdjustment(1, 12);

        Assert.Equal(0, maxGain);
        Assert.Equal(-20, penalty); // Severe late-game penalty
        Assert.Contains("expected 2", reason!);
    }
}
