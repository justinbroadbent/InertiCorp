namespace InertiCorp.Core;

/// <summary>
/// Immutable record representing the organization's current state.
/// All meters are clamped to 0-100 at construction.
/// </summary>
public sealed record OrgState
{
    public int Delivery { get; }
    public int Morale { get; }
    public int Governance { get; }
    public int Alignment { get; }
    public int Runway { get; }

    public OrgState(int Delivery, int Morale, int Governance, int Alignment, int Runway)
    {
        this.Delivery = Clamp(Delivery);
        this.Morale = Clamp(Morale);
        this.Governance = Clamp(Governance);
        this.Alignment = Clamp(Alignment);
        this.Runway = Clamp(Runway);
    }

    /// <summary>
    /// Default starting state with all meters at 60.
    /// Starting higher gives players buffer to learn and explore content.
    /// </summary>
    public static OrgState Default => new(
        Delivery: 60,
        Morale: 60,
        Governance: 60,
        Alignment: 60,
        Runway: 60
    );

    /// <summary>
    /// Returns a new OrgState with the specified meter changed by delta.
    /// Result is clamped to 0-100.
    /// </summary>
    public OrgState WithMeterChange(Meter meter, int delta)
    {
        return meter switch
        {
            Meter.Delivery => new OrgState(Delivery + delta, Morale, Governance, Alignment, Runway),
            Meter.Morale => new OrgState(Delivery, Morale + delta, Governance, Alignment, Runway),
            Meter.Governance => new OrgState(Delivery, Morale, Governance + delta, Alignment, Runway),
            Meter.Alignment => new OrgState(Delivery, Morale, Governance, Alignment + delta, Runway),
            Meter.Runway => new OrgState(Delivery, Morale, Governance, Alignment, Runway + delta),
            _ => throw new ArgumentOutOfRangeException(nameof(meter))
        };
    }

    /// <summary>
    /// Gets the value of the specified meter.
    /// </summary>
    public int GetMeter(Meter meter)
    {
        return meter switch
        {
            Meter.Delivery => Delivery,
            Meter.Morale => Morale,
            Meter.Governance => Governance,
            Meter.Alignment => Alignment,
            Meter.Runway => Runway,
            _ => throw new ArgumentOutOfRangeException(nameof(meter))
        };
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);  // Meters capped at 0-100
}
