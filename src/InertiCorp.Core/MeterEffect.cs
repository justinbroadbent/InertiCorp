namespace InertiCorp.Core;

/// <summary>
/// An effect that changes a specific meter by a delta value.
/// </summary>
public sealed class MeterEffect : IEffect
{
    public Meter Meter { get; }
    public int Delta { get; }

    public MeterEffect(Meter meter, int delta)
    {
        Meter = meter;
        Delta = delta;
    }

    public (GameState NewState, IEnumerable<LogEntry> Entries) Apply(GameState state, IRng rng)
    {
        var newOrg = state.Org.WithMeterChange(Meter, Delta);
        var newState = state.WithOrg(newOrg);

        var sign = Delta >= 0 ? "+" : "";
        var message = $"{Meter} {sign}{Delta}";
        var entry = LogEntry.MeterChange(Meter, Delta, message);

        return (newState, new[] { entry });
    }
}
