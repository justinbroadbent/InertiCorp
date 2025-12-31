namespace InertiCorp.Core.Tests;

public class MeterEffectTests
{
    [Fact]
    public void Apply_ReducesMeter()
    {
        var state = GameState.NewGame(seed: 123); // All meters at 60
        var effect = new MeterEffect(Meter.Morale, -10);
        var rng = new SeededRng(123);

        var (newState, _) = effect.Apply(state, rng);

        Assert.Equal(50, newState.Org.Morale); // 60 - 10 = 50
    }

    [Fact]
    public void Apply_IncreasesMeter()
    {
        var state = GameState.NewGame(seed: 123); // All meters at 60
        var effect = new MeterEffect(Meter.Delivery, 15);
        var rng = new SeededRng(123);

        var (newState, _) = effect.Apply(state, rng);

        Assert.Equal(75, newState.Org.Delivery); // 60 + 15 = 75
    }

    [Fact]
    public void Apply_ClampsToZero()
    {
        var state = GameState.NewGame(seed: 123); // Morale at 60
        var effect = new MeterEffect(Meter.Morale, -100);
        var rng = new SeededRng(123);

        var (newState, _) = effect.Apply(state, rng);

        Assert.Equal(0, newState.Org.Morale);
    }

    [Fact]
    public void Apply_CapsAt100()
    {
        // Meters are capped at 100
        var state = GameState.NewGame(seed: 123); // Delivery at 60
        var effect = new MeterEffect(Meter.Delivery, 100);
        var rng = new SeededRng(123);

        var (newState, _) = effect.Apply(state, rng);

        Assert.Equal(100, newState.Org.Delivery); // 60 + 100 = 160, but capped at 100
    }

    [Fact]
    public void Apply_ReturnsLogEntryWithMeterAndDelta()
    {
        var state = GameState.NewGame(seed: 123);
        var effect = new MeterEffect(Meter.Governance, -5);
        var rng = new SeededRng(123);

        var (_, entries) = effect.Apply(state, rng);

        var entry = entries.Single();
        Assert.Equal(LogCategory.MeterChange, entry.Category);
        Assert.Equal(Meter.Governance, entry.Meter);
        Assert.Equal(-5, entry.Delta);
    }

    [Fact]
    public void Apply_DoesNotMutateInputState()
    {
        var original = GameState.NewGame(seed: 123);
        var effect = new MeterEffect(Meter.Runway, -20);
        var rng = new SeededRng(123);

        var (newState, _) = effect.Apply(original, rng);

        Assert.Equal(60, original.Org.Runway); // Original unchanged
        Assert.Equal(40, newState.Org.Runway); // New state changed
    }

    [Fact]
    public void Apply_IsDeterministic()
    {
        var state1 = GameState.NewGame(seed: 456);
        var state2 = GameState.NewGame(seed: 456);
        var effect = new MeterEffect(Meter.Alignment, 10);
        var rng1 = new SeededRng(456);
        var rng2 = new SeededRng(456);

        var (newState1, entries1) = effect.Apply(state1, rng1);
        var (newState2, entries2) = effect.Apply(state2, rng2);

        Assert.Equal(newState1.Org.Alignment, newState2.Org.Alignment);
        Assert.Equal(entries1.Count(), entries2.Count());
    }

    [Fact]
    public void Apply_AllMeters_Work()
    {
        var rng = new SeededRng(789);

        foreach (Meter meter in Enum.GetValues<Meter>())
        {
            var state = GameState.NewGame(seed: 789);
            var effect = new MeterEffect(meter, -10);

            var (newState, _) = effect.Apply(state, rng);

            Assert.Equal(50, newState.Org.GetMeter(meter)); // 60 - 10 = 50
        }
    }

    [Fact]
    public void LogEntry_HasDescriptiveMessage()
    {
        var state = GameState.NewGame(seed: 123);
        var effect = new MeterEffect(Meter.Morale, -10);
        var rng = new SeededRng(123);

        var (_, entries) = effect.Apply(state, rng);

        var entry = entries.Single();
        Assert.Contains("Morale", entry.Message);
        Assert.Contains("-10", entry.Message);
    }
}
