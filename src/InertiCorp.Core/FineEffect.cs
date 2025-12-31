namespace InertiCorp.Core;

/// <summary>
/// An effect that imposes a legal/regulatory fine (in millions).
/// Fines are deducted from quarterly profit and reported separately.
/// </summary>
public sealed class FineEffect : IEffect
{
    /// <summary>
    /// The fine amount in millions (always positive, represents money lost).
    /// </summary>
    public int Amount { get; }

    /// <summary>
    /// Description of what the fine is for.
    /// </summary>
    public string Reason { get; }

    public FineEffect(int amount, string reason = "Legal settlement")
    {
        Amount = amount > 0 ? amount : 0;
        Reason = reason;
    }

    public (GameState NewState, IEnumerable<LogEntry> Entries) Apply(GameState state, IRng rng)
    {
        // FineEffect is handled specially in QuarterLoopEngine since it affects quarter financials
        // This Apply just returns unchanged state with a log entry
        var message = $"Fine: ${Amount}M ({Reason})";
        var entry = LogEntry.Info(message);

        return (state, new[] { entry });
    }
}
