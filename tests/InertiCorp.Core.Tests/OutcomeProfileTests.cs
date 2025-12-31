namespace InertiCorp.Core.Tests;

public class OutcomeProfileTests
{
    [Fact]
    public void OutcomeTier_HasThreeValues()
    {
        var tiers = Enum.GetValues<OutcomeTier>();
        Assert.Equal(3, tiers.Length);
        Assert.Contains(OutcomeTier.Good, tiers);
        Assert.Contains(OutcomeTier.Expected, tiers);
        Assert.Contains(OutcomeTier.Bad, tiers);
    }

    [Fact]
    public void OutcomeProfile_HasThreeTiers()
    {
        var profile = new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );

        Assert.NotNull(profile.Good);
        Assert.NotNull(profile.Expected);
        Assert.NotNull(profile.Bad);
    }

    [Fact]
    public void OutcomeProfile_GetEffectsForTier_ReturnsCorrectEffects()
    {
        var goodEffects = new IEffect[] { new MeterEffect(Meter.Delivery, 15) };
        var expectedEffects = new IEffect[] { new MeterEffect(Meter.Delivery, 10) };
        var badEffects = new IEffect[] { new MeterEffect(Meter.Delivery, 5) };

        var profile = new OutcomeProfile(goodEffects, expectedEffects, badEffects);

        Assert.Same(goodEffects, profile.GetEffectsForTier(OutcomeTier.Good));
        Assert.Same(expectedEffects, profile.GetEffectsForTier(OutcomeTier.Expected));
        Assert.Same(badEffects, profile.GetEffectsForTier(OutcomeTier.Bad));
    }

    [Fact]
    public void OutcomeProfile_Roll_ReturnsValidTier()
    {
        var profile = new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );

        var tier = profile.Roll(new SeededRng(42), goodWeight: 20, expectedWeight: 60, badWeight: 20);

        Assert.True(Enum.IsDefined(tier));
    }

    [Fact]
    public void OutcomeProfile_Roll_IsDeterministic()
    {
        var profile = new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Delivery, 15) },
            Expected: new[] { new MeterEffect(Meter.Delivery, 10) },
            Bad: new[] { new MeterEffect(Meter.Delivery, 5) }
        );

        var tier1 = profile.Roll(new SeededRng(42), 20, 60, 20);
        var tier2 = profile.Roll(new SeededRng(42), 20, 60, 20);

        Assert.Equal(tier1, tier2);
    }

    [Fact]
    public void OutcomeProfile_Roll_RespectsWeights()
    {
        var profile = new OutcomeProfile(
            Good: Array.Empty<IEffect>(),
            Expected: Array.Empty<IEffect>(),
            Bad: Array.Empty<IEffect>()
        );

        // Roll 100 times with heavily weighted Expected
        var counts = new Dictionary<OutcomeTier, int>
        {
            [OutcomeTier.Good] = 0,
            [OutcomeTier.Expected] = 0,
            [OutcomeTier.Bad] = 0
        };

        for (int i = 0; i < 100; i++)
        {
            var tier = profile.Roll(new SeededRng(i), goodWeight: 10, expectedWeight: 80, badWeight: 10);
            counts[tier]++;
        }

        // Expected should be most common
        Assert.True(counts[OutcomeTier.Expected] > counts[OutcomeTier.Good]);
        Assert.True(counts[OutcomeTier.Expected] > counts[OutcomeTier.Bad]);
    }

    [Fact]
    public void OutcomeProfile_Roll_100PercentGood_AlwaysGood()
    {
        var profile = new OutcomeProfile(
            Good: Array.Empty<IEffect>(),
            Expected: Array.Empty<IEffect>(),
            Bad: Array.Empty<IEffect>()
        );

        for (int i = 0; i < 20; i++)
        {
            var tier = profile.Roll(new SeededRng(i), goodWeight: 100, expectedWeight: 0, badWeight: 0);
            Assert.Equal(OutcomeTier.Good, tier);
        }
    }
}
