namespace InertiCorp.Core.Tests;

/// <summary>
/// Tests for the d20-based ouster calculator.
/// Risk is calculated as threshold * 5 (d20 roll <= threshold = ousted).
/// </summary>
public class OusterCalculatorTests
{
    [Fact]
    public void GetOusterRisk_HighFavorability_ZeroRisk()
    {
        // Favorability >= 55 is the safe zone
        var risk = OusterCalculator.GetOusterRisk(favorability: 80, pressureLevel: 5);
        Assert.Equal(0, risk);
    }

    [Fact]
    public void GetOusterRisk_Favorability55_ZeroRisk()
    {
        // Exactly 55 is the safe zone boundary
        var risk = OusterCalculator.GetOusterRisk(favorability: 55, pressureLevel: 10);
        Assert.Equal(0, risk);
    }

    [Fact]
    public void GetOusterRisk_CautionZone_BaseRisk5Percent()
    {
        // Favorability 40-54: caution zone, base threshold 1 (5%)
        // With quartersSurvived=0 (honeymoon -4), threshold becomes 0
        var risk = OusterCalculator.GetOusterRisk(favorability: 50, pressureLevel: 0, quartersSurvived: 0, evilScore: 10);
        Assert.Equal(0, risk); // Honeymoon protection only

        // After honeymoon (Q9+), no honeymoon bonus, high evil = no ethics bonus
        var riskAfterHoneymoon = OusterCalculator.GetOusterRisk(favorability: 50, pressureLevel: 0, quartersSurvived: 9, evilScore: 10);
        Assert.Equal(5, riskAfterHoneymoon); // Base 5% (threshold 1)
    }

    [Fact]
    public void GetOusterRisk_DangerZone_BaseRisk10Percent()
    {
        // Favorability 25-39: danger zone, base threshold 2 (10%)
        var risk = OusterCalculator.GetOusterRisk(favorability: 35, pressureLevel: 0, quartersSurvived: 9, evilScore: 10);
        Assert.Equal(10, risk); // Base 10% (threshold 2)
    }

    [Fact]
    public void GetOusterRisk_HighDangerZone_BaseRisk15Percent()
    {
        // Favorability 10-24: high danger, base threshold 3 (15%)
        var risk = OusterCalculator.GetOusterRisk(favorability: 20, pressureLevel: 0, quartersSurvived: 9, evilScore: 10);
        Assert.Equal(15, risk); // Base 15% (threshold 3)
    }

    [Fact]
    public void GetOusterRisk_CriticalZone_BaseRisk20Percent()
    {
        // Favorability < 10: critical, base threshold 4 (20%)
        var risk = OusterCalculator.GetOusterRisk(favorability: 5, pressureLevel: 0, quartersSurvived: 9, evilScore: 10);
        Assert.Equal(20, risk); // Base 20% (threshold 4)
    }

    [Fact]
    public void GetOusterRisk_PressureAddsToThreshold()
    {
        // Each 2 pressure levels adds +1 to threshold (+5% risk)
        var riskP0 = OusterCalculator.GetOusterRisk(favorability: 50, pressureLevel: 0, quartersSurvived: 9, evilScore: 10);
        var riskP4 = OusterCalculator.GetOusterRisk(favorability: 50, pressureLevel: 4, quartersSurvived: 9, evilScore: 10);

        Assert.Equal(5, riskP0);  // Threshold 1
        Assert.Equal(15, riskP4); // Threshold 1 + 4/2 = 3
    }

    [Fact]
    public void GetOusterRisk_CappedAt70Percent()
    {
        // Cap at threshold 14 (70% chance) - always a glimmer of hope
        // Use high evil to avoid ethics bonus (-2 from evil 0)
        var risk = OusterCalculator.GetOusterRisk(favorability: 0, pressureLevel: 20, quartersSurvived: 20, evilScore: 10);
        Assert.Equal(70, risk);
    }

    [Fact]
    public void GetOusterRisk_HoneymoonProtection()
    {
        // Use evilScore: 10 to avoid ethics bonus
        // Base: danger zone (30) = 2, pressure 2/2 = 1, total = 3
        // Q1-3: -4 to threshold
        var riskQ1 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 1, evilScore: 10);
        // Q4-5: -2 to threshold
        var riskQ5 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 5, evilScore: 10);
        // Q6-7: -1 to threshold
        var riskQ7 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 7, evilScore: 10);
        // Q8+: no honeymoon
        var riskQ9 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 10);

        Assert.Equal(0, riskQ1);  // 2 + 1 - 4 = 0
        Assert.Equal(5, riskQ5);  // 2 + 1 - 2 = 1
        Assert.Equal(10, riskQ7); // 2 + 1 - 1 = 2
        Assert.Equal(15, riskQ9); // 2 + 1 = 3
    }

    [Fact]
    public void GetOusterRisk_EthicsBonus()
    {
        // Base: danger zone (30) = 2, pressure 2/2 = 1, total = 3
        // Evil 0: -2 to threshold
        var riskEvil0 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 0);
        // Evil 1-4: -1 to threshold
        var riskEvil3 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 3);
        // Evil 5+: no bonus
        var riskEvil7 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 7);
        var riskEvil15 = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 15);

        Assert.Equal(5, riskEvil0);   // 3 - 2 = 1
        Assert.Equal(10, riskEvil3);  // 3 - 1 = 2
        Assert.Equal(15, riskEvil7);  // 3 - 0 = 3
        Assert.Equal(15, riskEvil15); // 3 - 0 = 3
    }

    [Fact]
    public void GetOusterRisk_PerformanceBonus()
    {
        // Base: danger zone (30) = 2, pressure 2/2 = 1, total = 3
        // DirectiveMet: -2, profitGrew (legacy): sets both profitPositive -1 and profitImproving -1
        var baseRisk = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 10);
        var riskDirectiveMet = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 10, directiveMet: true);
        var riskProfitGrew = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 10, profitGrew: true);
        var riskBoth = OusterCalculator.GetOusterRisk(favorability: 30, pressureLevel: 2, quartersSurvived: 9, evilScore: 10, directiveMet: true, profitGrew: true);

        Assert.Equal(15, baseRisk);        // Threshold 3
        Assert.Equal(5, riskDirectiveMet); // Threshold 3 - 2 = 1
        Assert.Equal(5, riskProfitGrew);   // Threshold 3 - 1 - 1 = 1 (legacy maps to both bonuses)
        Assert.Equal(0, riskBoth);         // Threshold 3 - 2 - 1 - 1 = 0 (capped at 0)
    }

    [Fact]
    public void RollForOuster_SafeZone_NeverOusted()
    {
        // Favorability >= 55 should never cause ouster
        for (int i = 0; i < 100; i++)
        {
            var result = OusterCalculator.RollForOuster(favorability: 80, pressureLevel: 10, new SeededRng(i));
            Assert.False(result);
        }
    }

    [Fact]
    public void RollForOuster_ZeroThreshold_NeverOusted()
    {
        // Even with low favorability, honeymoon can bring threshold to 0
        for (int i = 0; i < 20; i++)
        {
            var result = OusterCalculator.RollForOuster(
                favorability: 50, pressureLevel: 1, new SeededRng(i),
                quartersSurvived: 1, evilScore: 0); // Honeymoon + ethics = safe
            Assert.False(result);
        }
    }

    [Fact]
    public void RollForOuster_IsDeterministic()
    {
        var result1 = OusterCalculator.RollForOuster(favorability: 40, pressureLevel: 3, new SeededRng(12345));
        var result2 = OusterCalculator.RollForOuster(favorability: 40, pressureLevel: 3, new SeededRng(12345));

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetRiskDescription_SafeZone_ReturnsConfident()
    {
        var desc = OusterCalculator.GetRiskDescription(favorability: 75, pressureLevel: 2);
        Assert.Contains("Safe", desc);
        Assert.Contains("confident", desc);
    }

    [Fact]
    public void GetRiskDescription_IncludesModifierNotes()
    {
        var desc = OusterCalculator.GetRiskDescription(
            favorability: 45, pressureLevel: 1, quartersSurvived: 3, evilScore: 2, directiveMet: true);

        Assert.Contains("honeymoon", desc);
        Assert.Contains("ethical", desc);
        Assert.Contains("performing", desc);
    }
}
