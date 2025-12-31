namespace InertiCorp.Core.Content;

/// <summary>
/// Placeholder objectives for UI development and testing.
/// </summary>
public static class PlaceholderObjectives
{
    /// <summary>
    /// Keep Morale objective - Morale must be >= 40 at end of game.
    /// </summary>
    public static Objective KeepMorale { get; } = new Objective(
        "OBJ_PLACEHOLDER_001_KEEP_MORALE",
        "Maintain Morale",
        "Keep team Morale at 40 or above",
        new MeterThresholdCondition(Meter.Morale, 40)
    );

    /// <summary>
    /// Hit Delivery objective - Delivery must be >= 60 at end of game.
    /// </summary>
    public static Objective HitDelivery { get; } = new Objective(
        "OBJ_PLACEHOLDER_002_HIT_DELIVERY",
        "Hit Delivery Target",
        "Achieve Delivery score of 60 or above",
        new MeterThresholdCondition(Meter.Delivery, 60)
    );

    /// <summary>
    /// All placeholder objectives.
    /// </summary>
    public static IReadOnlyList<Objective> All { get; } = new[]
    {
        KeepMorale,
        HitDelivery
    };
}
