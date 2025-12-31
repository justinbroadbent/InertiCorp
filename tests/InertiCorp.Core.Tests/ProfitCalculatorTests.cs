namespace InertiCorp.Core.Tests;

public class ProfitCalculatorTests
{
    [Fact]
    public void CalculateBaseOperations_ReturnsPositiveInNormalConditions()
    {
        var org = new OrgState(Morale: 50, Runway: 50, Alignment: 50, Delivery: 50, Governance: 50);

        // Run multiple times to avoid hitting the 8% bad quarter chance
        var profits = Enumerable.Range(0, 50)
            .Select(i => ProfitCalculator.CalculateBaseOperations(org, new SeededRng(i)))
            .ToList();

        // Most should be positive (allowing for 8% bad quarter chance)
        var positiveCount = profits.Count(p => p > 0);
        Assert.True(positiveCount >= 40, $"Expected at least 40 positive results, got {positiveCount}");
    }

    [Fact]
    public void CalculateBaseOperations_HighDelivery_GivesBonus()
    {
        var orgLow = new OrgState(Morale: 50, Runway: 50, Alignment: 50, Delivery: 30, Governance: 50);
        var orgHigh = new OrgState(Morale: 50, Runway: 50, Alignment: 50, Delivery: 70, Governance: 50);

        // Use seeds that don't trigger bad quarter (avoid 0-7 range check)
        var profitsLow = Enumerable.Range(100, 30)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgLow, new SeededRng(i)))
            .Average();
        var profitsHigh = Enumerable.Range(100, 30)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgHigh, new SeededRng(i)))
            .Average();

        Assert.True(profitsHigh > profitsLow, $"High delivery {profitsHigh} should beat low {profitsLow}");
    }

    [Fact]
    public void CalculateBaseOperations_HighRunway_GivesBonus()
    {
        var orgLow = new OrgState(Morale: 50, Runway: 30, Alignment: 50, Delivery: 50, Governance: 50);
        var orgHigh = new OrgState(Morale: 50, Runway: 70, Alignment: 50, Delivery: 50, Governance: 50);

        var profitsLow = Enumerable.Range(100, 30)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgLow, new SeededRng(i)))
            .Average();
        var profitsHigh = Enumerable.Range(100, 30)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgHigh, new SeededRng(i)))
            .Average();

        Assert.True(profitsHigh > profitsLow, $"High runway {profitsHigh} should beat low {profitsLow}");
    }

    [Fact]
    public void CalculateBaseOperations_CanProduceNegative()
    {
        var org = new OrgState(Morale: 50, Runway: 50, Alignment: 50, Delivery: 50, Governance: 50);

        // Run many times to hit the 8% bad quarter chance
        var profits = Enumerable.Range(0, 200)
            .Select(i => ProfitCalculator.CalculateBaseOperations(org, new SeededRng(i)))
            .ToList();

        var hasNegative = profits.Any(p => p < 0);
        Assert.True(hasNegative, "Should occasionally produce negative profit (market downturns)");
    }

    [Fact]
    public void CalculateBaseOperations_IsDeterministic()
    {
        var org = new OrgState(Morale: 50, Runway: 50, Alignment: 50, Delivery: 50, Governance: 50);

        var profit1 = ProfitCalculator.CalculateBaseOperations(org, new SeededRng(42));
        var profit2 = ProfitCalculator.CalculateBaseOperations(org, new SeededRng(42));

        Assert.Equal(profit1, profit2);
    }

    [Fact]
    public void CalculateTotal_SumsBaseAndProject()
    {
        var total = ProfitCalculator.CalculateTotal(100, 25);
        Assert.Equal(125, total);

        var negative = ProfitCalculator.CalculateTotal(100, -30);
        Assert.Equal(70, negative);
    }

    [Fact]
    public void Format_PositiveProfit_ReturnsCorrectFormat()
    {
        Assert.Equal("$150M", ProfitCalculator.Format(150));
        Assert.Equal("$1.5B", ProfitCalculator.Format(1500));
    }

    [Fact]
    public void Format_NegativeProfit_ReturnsCorrectFormat()
    {
        Assert.Equal("-$30M", ProfitCalculator.Format(-30));
        Assert.Equal("-$1.2B", ProfitCalculator.Format(-1200));
    }

    [Fact]
    public void FormatWithSign_AddsExplicitSign()
    {
        Assert.Equal("+$50M", ProfitCalculator.FormatWithSign(50));
        Assert.Equal("-$20M", ProfitCalculator.FormatWithSign(-20));
        Assert.Equal("+$0M", ProfitCalculator.FormatWithSign(0));
    }

    [Fact]
    public void CalculateBaseOperations_HighMeters_ProducesHigherProfit()
    {
        var orgLow = new OrgState(Morale: 30, Runway: 30, Alignment: 30, Delivery: 30, Governance: 30);
        var orgHigh = new OrgState(Morale: 80, Runway: 80, Alignment: 80, Delivery: 80, Governance: 80);

        // Average over many runs to smooth out variance
        var profitsLow = Enumerable.Range(100, 50)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgLow, new SeededRng(i)))
            .Average();
        var profitsHigh = Enumerable.Range(100, 50)
            .Select(i => ProfitCalculator.CalculateBaseOperations(orgHigh, new SeededRng(i)))
            .Average();

        Assert.True(profitsHigh > profitsLow, $"High meters {profitsHigh} should beat low {profitsLow}");
    }
}
