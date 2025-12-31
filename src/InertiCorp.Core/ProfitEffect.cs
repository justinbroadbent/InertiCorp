namespace InertiCorp.Core;

/// <summary>
/// An effect that changes quarterly profit by a delta value (in millions).
/// Positive = profit boost, Negative = profit hit.
/// </summary>
public sealed class ProfitEffect : IEffect
{
    public int Delta { get; }

    public ProfitEffect(int delta)
    {
        Delta = delta;
    }

    public (GameState NewState, IEnumerable<LogEntry> Entries) Apply(GameState state, IRng rng)
    {
        // ProfitEffect is handled specially in QuarterEngine since it affects CEOState
        // This Apply just returns unchanged state with a log entry
        var sign = Delta >= 0 ? "+" : "";
        var message = $"Profit {sign}${Delta}M";
        var entry = LogEntry.Info(message);

        return (state, new[] { entry });
    }
}
