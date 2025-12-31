namespace InertiCorp.Core.Tests;

public class ChoiceForecastTests
{
    [Fact]
    public void Create_HasLikelyOutcome_FromExpectedTier()
    {
        var profile = new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );

        var forecast = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 1);

        Assert.NotNull(forecast.LikelyOutcome);
        Assert.Contains("Delivery", forecast.LikelyOutcome);
    }

    [Fact]
    public void RiskLevel_LowAlignment_HighRisk()
    {
        var profile = CreateSimpleProfile();

        var forecastLow = ChoiceForecast.Create(profile, alignment: 20, pressureLevel: 1);
        var forecastHigh = ChoiceForecast.Create(profile, alignment: 80, pressureLevel: 1);

        Assert.True(forecastLow.RiskLevel >= forecastHigh.RiskLevel);
    }

    [Fact]
    public void RiskLevel_HighPressure_HighRisk()
    {
        var profile = CreateSimpleProfile();

        var forecastLow = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 1);
        var forecastHigh = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 5);

        Assert.True(forecastHigh.RiskLevel >= forecastLow.RiskLevel);
    }

    [Fact]
    public void RiskLevel_IsLowMedOrHigh()
    {
        var profile = CreateSimpleProfile();

        var forecast = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 3);

        Assert.True(forecast.RiskLevel >= RiskLevel.Low && forecast.RiskLevel <= RiskLevel.High);
    }

    [Fact]
    public void Create_IsConsistent_SameInputsSameOutput()
    {
        var profile = CreateSimpleProfile();

        var forecast1 = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 3);
        var forecast2 = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 3);

        Assert.Equal(forecast1.RiskLevel, forecast2.RiskLevel);
        Assert.Equal(forecast1.LikelyOutcome, forecast2.LikelyOutcome);
    }

    [Fact]
    public void Create_RequiresNoRng()
    {
        var profile = CreateSimpleProfile();

        // This should not throw - forecast is deterministic, no RNG needed
        var forecast = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 1);

        Assert.NotNull(forecast);
    }

    [Fact]
    public void MeterRange_ShowsMinMaxAcrossTiers()
    {
        var profile = new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );

        var forecast = ChoiceForecast.Create(profile, alignment: 50, pressureLevel: 1);

        Assert.True(forecast.MeterRanges.ContainsKey(Meter.Delivery));
        var range = forecast.MeterRanges[Meter.Delivery];
        Assert.Equal(5, range.Min);
        Assert.Equal(15, range.Max);
    }

    private static OutcomeProfile CreateSimpleProfile()
    {
        return new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );
    }
}
