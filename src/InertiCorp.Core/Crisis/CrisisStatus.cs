namespace InertiCorp.Core.Crisis;

/// <summary>
/// Status of a crisis instance.
/// </summary>
public enum CrisisStatus
{
    /// <summary>
    /// Crisis is ongoing and requires attention.
    /// </summary>
    Active,

    /// <summary>
    /// Crisis was successfully mitigated through player action.
    /// </summary>
    Mitigated,

    /// <summary>
    /// Crisis escalated due to failed mitigation or expiration.
    /// </summary>
    Escalated,

    /// <summary>
    /// Crisis deadline passed without resolution.
    /// </summary>
    Expired
}

/// <summary>
/// Quality of staff assigned to handle a crisis response.
/// Affects outcome messaging and can spawn additional effects.
/// </summary>
public enum StaffQuality
{
    /// <summary>
    /// Competent team that executes well.
    /// </summary>
    Good,

    /// <summary>
    /// Average team with mixed results.
    /// </summary>
    Meh,

    /// <summary>
    /// Incompetent PM who creates additional problems.
    /// </summary>
    Inept
}
