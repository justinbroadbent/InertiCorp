namespace InertiCorp.Core;

/// <summary>
/// Condition that checks if a specific meter is at or above a threshold.
/// </summary>
public sealed class MeterThresholdCondition : IObjectiveCondition
{
    /// <summary>
    /// The meter to check.
    /// </summary>
    public Meter Meter { get; }

    /// <summary>
    /// The minimum threshold value (inclusive).
    /// </summary>
    public int Threshold { get; }

    public MeterThresholdCondition(Meter meter, int threshold)
    {
        Meter = meter;
        Threshold = threshold;
    }

    /// <inheritdoc />
    public bool IsMet(OrgState state)
    {
        var value = Meter switch
        {
            Meter.Delivery => state.Delivery,
            Meter.Morale => state.Morale,
            Meter.Governance => state.Governance,
            Meter.Alignment => state.Alignment,
            Meter.Runway => state.Runway,
            _ => throw new ArgumentOutOfRangeException(nameof(Meter))
        };

        return value >= Threshold;
    }
}
