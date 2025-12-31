namespace InertiCorp.Core.Situation;

/// <summary>
/// Severity level of a situation, affecting urgency and consequences.
/// </summary>
public enum SituationSeverity
{
    /// <summary>Minor inconvenience - easily managed</summary>
    Minor = 1,

    /// <summary>Moderate concern - requires attention</summary>
    Moderate = 2,

    /// <summary>Major issue - significant impact if mishandled</summary>
    Major = 3,

    /// <summary>Critical emergency - cannot be deferred</summary>
    Critical = 4
}
