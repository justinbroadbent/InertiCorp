namespace InertiCorp.Core.Content;

/// <summary>
/// Game objectives - goals to achieve by end of quarter.
/// </summary>
public static class GameObjectives
{
    /// <summary>
    /// Ship Something Real - requires high delivery performance.
    /// </summary>
    public static Objective ShipSomethingReal { get; } = new Objective(
        "OBJ_001_SHIP_SOMETHING",
        "Ship Something Real",
        "Achieve a Delivery score of 60 or higher",
        new MeterThresholdCondition(Meter.Delivery, 60)
    );

    /// <summary>
    /// Pass the Audit - requires strong governance.
    /// </summary>
    public static Objective PassTheAudit { get; } = new Objective(
        "OBJ_002_PASS_AUDIT",
        "Pass the Audit",
        "Achieve a Governance score of 60 or higher",
        new MeterThresholdCondition(Meter.Governance, 60)
    );

    /// <summary>
    /// Retain Key Staff - requires good morale.
    /// </summary>
    public static Objective RetainKeyStaff { get; } = new Objective(
        "OBJ_003_RETAIN_STAFF",
        "Retain Key Staff",
        "Maintain Morale at 50 or higher",
        new MeterThresholdCondition(Meter.Morale, 50)
    );

    /// <summary>
    /// Reduce Organizational Chaos - requires alignment.
    /// </summary>
    public static Objective ReduceOrganizationalChaos { get; } = new Objective(
        "OBJ_004_REDUCE_CHAOS",
        "Reduce Organizational Chaos",
        "Achieve Alignment of 50 or higher",
        new MeterThresholdCondition(Meter.Alignment, 50)
    );

    /// <summary>
    /// Extend the Runway - requires budget management.
    /// </summary>
    public static Objective ExtendRunway { get; } = new Objective(
        "OBJ_005_EXTEND_RUNWAY",
        "Extend the Runway",
        "Maintain Runway at 40 or higher",
        new MeterThresholdCondition(Meter.Runway, 40)
    );

    /// <summary>
    /// Balanced Quarter - requires all meters to be healthy.
    /// </summary>
    public static Objective BalancedQuarter { get; } = new Objective(
        "OBJ_006_BALANCED_QUARTER",
        "Balanced Quarter",
        "End with all meters at 40 or higher",
        CompositeCondition.And(
            new MeterThresholdCondition(Meter.Morale, 40),
            new MeterThresholdCondition(Meter.Runway, 40),
            new MeterThresholdCondition(Meter.Alignment, 40),
            new MeterThresholdCondition(Meter.Delivery, 40),
            new MeterThresholdCondition(Meter.Governance, 40)
        )
    );

    /// <summary>
    /// All game objectives.
    /// </summary>
    public static IReadOnlyList<Objective> All { get; } = new[]
    {
        ShipSomethingReal,
        PassTheAudit,
        RetainKeyStaff,
        ReduceOrganizationalChaos,
        ExtendRunway,
        BalancedQuarter
    };
}
