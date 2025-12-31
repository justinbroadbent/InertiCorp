namespace InertiCorp.Core.Tests;

public class BoardDirectiveTests
{
    [Fact]
    public void ProfitRequirement_ScalesWithPressure()
    {
        var directive = BoardDirective.ProfitIncrease;

        var reqP1 = directive.GetRequiredAmount(pressureLevel: 1);
        var reqP3 = directive.GetRequiredAmount(pressureLevel: 3);
        var reqP5 = directive.GetRequiredAmount(pressureLevel: 5);

        Assert.True(reqP3 > reqP1);
        Assert.True(reqP5 > reqP3);
    }

    [Fact]
    public void ProfitRequirement_Formula_SqrtBased()
    {
        var directive = BoardDirective.ProfitIncrease;

        // Required = 5 + floor(sqrt(pressure * 8)) - diminishing growth for sustainability
        Assert.Equal(7, directive.GetRequiredAmount(pressureLevel: 1));   // 5 + floor(sqrt(8)) = 5 + 2 = 7
        Assert.Equal(9, directive.GetRequiredAmount(pressureLevel: 3));   // 5 + floor(sqrt(24)) = 5 + 4 = 9
        Assert.Equal(11, directive.GetRequiredAmount(pressureLevel: 5));  // 5 + floor(sqrt(40)) = 5 + 6 = 11
        Assert.Equal(15, directive.GetRequiredAmount(pressureLevel: 15)); // 5 + floor(sqrt(120)) = 5 + 10 = 15
        Assert.Equal(17, directive.GetRequiredAmount(pressureLevel: 20)); // 5 + floor(sqrt(160)) = 5 + 12 = 17
    }

    [Fact]
    public void IsMet_WhenProfitMeetsRequirement_ReturnsTrue()
    {
        var directive = BoardDirective.ProfitIncrease;
        var pressureLevel = 1;
        var required = directive.GetRequiredAmount(pressureLevel); // 7 (sqrt formula)

        var lastProfit = 100;
        var currentProfit = 107; // Exactly meets requirement

        Assert.True(directive.IsMet(lastProfit, currentProfit, pressureLevel));
    }

    [Fact]
    public void IsMet_WhenProfitBelowRequirement_ReturnsFalse()
    {
        var directive = BoardDirective.ProfitIncrease;
        var pressureLevel = 1;
        var required = directive.GetRequiredAmount(pressureLevel); // 7 (sqrt formula)

        var lastProfit = 100;
        var currentProfit = 106; // Below requirement (needs 7, only increased by 6)

        Assert.False(directive.IsMet(lastProfit, currentProfit, pressureLevel));
    }

    [Fact]
    public void IsMet_WhenProfitExceedsRequirement_ReturnsTrue()
    {
        var directive = BoardDirective.ProfitIncrease;
        var pressureLevel = 3;
        var required = directive.GetRequiredAmount(pressureLevel); // 9 (sqrt formula)

        var lastProfit = 100;
        var currentProfit = 150; // Well above requirement

        Assert.True(directive.IsMet(lastProfit, currentProfit, pressureLevel));
    }

    [Fact]
    public void DirectiveTitle_IsDescriptive()
    {
        var directive = BoardDirective.ProfitIncrease;

        Assert.False(string.IsNullOrEmpty(directive.Title));
        Assert.Contains("profit", directive.Title.ToLower());
    }

    [Fact]
    public void GetDescription_IncludesRequiredAmount()
    {
        var directive = BoardDirective.ProfitIncrease;

        var description = directive.GetDescription(pressureLevel: 3);

        Assert.Contains("9", description); // Required amount at pressure 3 (sqrt formula: 5 + floor(sqrt(24)) = 9)
    }
}
